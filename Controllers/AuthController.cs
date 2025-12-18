using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupplyChainAPI.Configuration;
using SupplyChainAPI.Models.Auth;
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

        // Конструктор контроллера
        public AuthController(ILogger<AuthController> logger, SupplyChainContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Аутентификация пользователя
        /// </summary>
        /// <param name="model">Модель запроса авторизации</param>
        /// <returns>Токен JWT и информация о пользователе</returns>
        [HttpPost]
        public async Task<ActionResult<LoginResponseModel>> Login([FromBody] LoginRequestModel model)
        {
            try
            {
                // Ищем пользователя в базе данных
                var user = await _context.Users
                    .FirstOrDefaultAsync(x => x.Login == model.Login && x.Password == model.Password);

                // Если пользователь не найден, возвращаем ошибку
                if (user == null)
                {
                    _logger.LogWarning("Попытка входа с неверными учетными данными для логина: {Login}", model.Login);
                    return Unauthorized(new LoginResponseModel
                    {
                        Status = 1,
                        Message = "Неверный логин или пароль"
                    });
                }

                // Создаем claims (утверждения) для JWT токена
                var claims = new List<Claim> {
                    new Claim(ClaimTypes.Name, user.Login),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("UserId", user.Id.ToString())
                };

                // Создаем JWT токен
                var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricSecurityKey(),
                        SecurityAlgorithms.HmacSha256)
                );

                // Генерируем строковое представление токена
                var token = new JwtSecurityTokenHandler().WriteToken(jwt);

                _logger.LogInformation("Пользователь {Login} успешно авторизован", user.Login);

                // Возвращаем успешный ответ
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
                // Логируем ошибку и возвращаем 500
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
        /// <param name="model">Модель запроса регистрации</param>
        /// <returns>Результат регистрации</returns>
        [HttpPost]
        public async Task<ActionResult<RegisterResponseModel>> Register([FromBody] RegisterRequestModel model)
        {
            try
            {
                // Проверяем валидность модели
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

                // Проверяем, не существует ли уже пользователь с таким логином
                bool userExists = await _context.Users
                    .AnyAsync(x => x.Login == model.Login);

                if (userExists)
                {
                    _logger.LogWarning("Попытка регистрации с существующим логином: {Login}", model.Login);
                    return Conflict(new RegisterResponseModel
                    {
                        Status = 1,
                        Message = "Пользователь с таким логином уже существует"
                    });
                }

                // Формируем имя пользователя
                string name = string.IsNullOrWhiteSpace(model.Name)
                    ? model.Login
                    : model.Name.Trim();

                // Создаем нового пользователя
                // ВНИМАНИЕ: В реальном приложении пароль должен быть захеширован!
                var newUser = new User
                {
                    Login = model.Login.Trim(),
                    Password = model.Password,
                    Name = name
                };

                // Добавляем пользователя в базу данных
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Новый пользователь зарегистрирован: {Login}", model.Login);

                // Возвращаем успешный ответ
                return Ok(new RegisterResponseModel
                {
                    Status = 0,
                    Message = "Регистрация выполнена успешно",
                    Login = newUser.Login
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Обработка ошибок базы данных
                _logger.LogError(dbEx, "Ошибка базы данных при регистрации пользователя {Login}", model.Login);
                return StatusCode(500, new RegisterResponseModel
                {
                    Status = 2,
                    Message = "Ошибка базы данных при регистрации"
                });
            }
            catch (Exception ex)
            {
                // Обработка общих ошибок
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