using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Simulation;

namespace NextFood.Components
{
    public class FoodCalculatorComponent
    {
        public static float getNewSkillPointsBalance(Player player, FoodItem food)
        {
            var stomach = player.User.Stomach;
            var itemsEaten = stomach.Contents.Select(c => c.Food).ToList();
            itemsEaten.Add(food);
            return getSkill(itemsEaten);
        }

        public static float getSkillPointsVariation(Player player, FoodItem food)
        {
            var stomach = player.User.Stomach;
            return getNewSkillPointsBalance(player, food) - stomach.NutrientSkillRate;
        }

        private static float getSkill(List<FoodItem> foodList)
        {
            Nutrients nutrientAvg = default(Nutrients);
            float totalCal = 0;

            foreach (var food in foodList)
            {
                if (food.Calories > 0)
                {
                    totalCal += food.Calories;
                    nutrientAvg += food.Nutrition * food.Calories;
                }
            }
            if (totalCal > 0)
            {
                nutrientAvg *= 1.0f / totalCal;
            }


            float BalanceBonus = nutrientAvg.Values.Max() > 0 ? (nutrientAvg.Values.Sum() / (nutrientAvg.Values.Max() * 4)) * 2 : 0;

            float NutrientSkillRate = ((nutrientAvg.NutrientTotal * BalanceBonus) + EcoSim.Obj.EcoDef.BaseSkillGainRate) * DifficultySettings.Obj.Config.DifficultyModifiers.SkillGainMultiplier;

            return NutrientSkillRate;
        }
    }
}
