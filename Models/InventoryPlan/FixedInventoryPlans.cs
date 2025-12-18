using System;
using System.Collections.Generic;

namespace SupplyChainAPI.Services
{
    public static class FixedInventoryPlans
    {
        /// <summary>
        /// Фиксированные планы для января 2023 года
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, decimal>> January2023Plans = new()
        {
            {
                "Торговое подразделение 1",
                new Dictionary<string, decimal>
                {
                    { "Готовая продукция 1", 150 },
                    { "Готовая продукция 2", 250 }
                }
            },
            {
                "Производственное подразделение 1",
                new Dictionary<string, decimal>
                {
                    { "Готовая продукция 1", 0 },
                    { "Готовая продукция 2", 0 },
                    { "Сырьё 1", 0 }
                }
            }
        };

        /// <summary>
        /// Получение фиксированного значения плана
        /// </summary>
        public static decimal? GetFixedPlanValue(DateTime date, string subdivisionName, string materialName)
        {
            // Проверяем, является ли это январем 2023
            if (date.Year == 2023 && date.Month == 1)
            {
                if (January2023Plans.TryGetValue(subdivisionName, out var materials) &&
                    materials.TryGetValue(materialName, out var value))
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Проверка, является ли план фиксированным
        /// </summary>
        public static bool IsFixedPlan(DateTime date, string subdivisionName, string materialName)
        {
            return GetFixedPlanValue(date, subdivisionName, materialName).HasValue;
        }

        /// <summary>
        /// Получение всех фиксированных значений для января 2023
        /// </summary>
        public static Dictionary<string, Dictionary<string, decimal>> GetJanuary2023FixedValues()
        {
            return January2023Plans;
        }

        /// <summary>
        /// Проверка, является ли подразделение производственным
        /// </summary>
        public static bool IsProductionSubdivision(string subdivisionName)
        {
            return subdivisionName?.Contains("Производственное") ?? false;
        }

        /// <summary>
        /// Проверка, является ли подразделение торговым
        /// </summary>
        public static bool IsTradingSubdivision(string subdivisionName)
        {
            return subdivisionName?.Contains("Торговое") ?? false;
        }

        /// <summary>
        /// Проверка, является ли материал сырьём
        /// </summary>
        public static bool IsRawMaterial(string materialName)
        {
            return materialName?.Contains("Сырьё") ?? false;
        }

        /// <summary>
        /// Проверка, является ли материал готовой продукцией
        /// </summary>
        public static bool IsFinishedProduct(string materialName)
        {
            return materialName?.Contains("Готовая продукция") ?? false;
        }
    }
}