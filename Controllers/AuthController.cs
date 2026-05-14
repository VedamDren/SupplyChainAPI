using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupplyChainAPI.Configuration;
using SupplyChainAPI.Models.Auth;
using SupplyChainAPI.Services;
using SupplyChainData;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SupplyChainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly SupplyChainContext _context;
        private readonly IPasswordService _passwordService; // новая зависимость

        // Конструктор с внедрением IPasswordService
        public AuthController(
            ILogger<AuthController> logger,
            SupplyChainContext context,
            IPasswordService passwordService)
        {
            _logger = logger;
            _context = context;
            _passwordService = passwordService;
        }

        /// <summary>
        /// Аутентификация пользователя
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LoginResponseModel>> Login([FromBody] LoginRequestModel model)
        {
            try
            {
                // 1. Ищем пользователя только по логину (пароль теперь хэш)
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Login == model.Login);

                // 2. Если пользователь не найден – сразу отказ
                if (user == null)
                {
                    _logger.LogWarning("Пользователь с логином {Login} не найден", model.Login);
                    return Unauthorized(new LoginResponseModel
                    {
                        Status = 1,
                        Message = "Неверный логин или пароль"
                    });
                }

                // 3. Проверяем хэш пароля
                bool isPasswordValid = _passwordService.VerifyPassword(model.Password, user.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Неверный пароль для пользователя {Login}", model.Login);
                    return Unauthorized(new LoginResponseModel
                    {
                        Status = 1,
                        Message = "Неверный логин или пароль"
                    });
                }

                // 4. Создаём claims для JWT
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("UserId", user.Id.ToString())
                };

                // 5. Генерируем JWT-токен
                var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricSecurityKey(),
                        SecurityAlgorithms.HmacSha256)
                );

                var token = new JwtSecurityTokenHandler().WriteToken(jwt);

                _logger.LogInformation("Пользователь {Login} успешно авторизован", user.Login);

                return Ok(new LoginResponseModel
                {
                    Status = 0,
                    Token = token,
                    Login = user.Login,
                    UserId = user.Id,
                    Message = "Успешная авторизация"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при авторизации пользователя {Login}", model.Login);
                return StatusCode(500, new LoginResponseModel
                {
                    Status = 2,
                    Message = "Внутренняя ошибка сервера"
                });
            }
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RegisterResponseModel>> Register([FromBody] RegisterRequestModel model)
        {
            try
            {
                // Валидация модели
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Ошибки валидации при регистрации: {Errors}", string.Join(", ", errors));

                    return BadRequest(new RegisterResponseModel
                    {
                        Status = 1,
                        Message = "Ошибки валидации: " + string.Join(", ", errors)
                    });
                }

                // Проверка на существование пользователя с таким логином
                bool userExists = await _context.Users.AnyAsync(x => x.Login == model.Login);
                if (userExists)
                {
                    _logger.LogWarning("Попытка регистрации с существующим логином: {Login}", model.Login);
                    return Conflict(new RegisterResponseModel
                    {
                        Status = 1,
                        Message = "Пользователь с таким логином уже существует"
                    });
                }

                // Формируем имя пользователя (если не задано, используем логин)
                string name = string.IsNullOrWhiteSpace(model.Name)
                    ? model.Login
                    : model.Name.Trim();

                // *** ВАЖНО: хэшируем пароль перед сохранением ***
                string passwordHash = _passwordService.HashPassword(model.Password);

                var newUser = new User
                {
                    Login = model.Login.Trim(),
                    Password = passwordHash,   // <-- теперь здесь хэш, а не открытый пароль
                    Name = name
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Новый пользователь зарегистрирован: {Login}", model.Login);

                return Ok(new RegisterResponseModel
                {
                    Status = 0,
                    Message = "Регистрация выполнена успешно",
                    Login = newUser.Login
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при регистрации пользователя {Login}", model.Login);
                return StatusCode(500, new RegisterResponseModel
                {
                    Status = 2,
                    Message = "Ошибка базы данных при регистрации"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неожиданная ошибка при регистрации пользователя {Login}", model.Login);
                return StatusCode(500, new RegisterResponseModel
                {
                    Status = 2,
                    Message = "Внутренняя ошибка сервера при регистрации"
                });
            }
        }
    }
}