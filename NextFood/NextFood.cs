/**
 * File: NextFood.cs
 * Eco Version: 9.x
 * 
 * Author: koka
 * 
 * 
 * Exports recipes to use in javascript
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Gameplay.Components;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.TextLinks;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using NextFood.Components;

namespace NextFood
{
    public class NextFood : IChatCommandHandler
    {
        public NextFood()
        {
        }

        /// <inheritdoc />
        public string GetStatus()
        {
            return "Idle.";
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return nameof(NextFood);
        }

        private delegate void EcoCommandFunction(User user, params string[] args);

        private static void CallWithErrorHandling<TRet>(EcoCommandFunction toCall, User user, params string[] args)
        {
            try
            {
                toCall(user, args);
            }
            catch (Exception e)
            {
                ChatManager.ServerMessageToPlayer(new LocString("Error occurred while attempting to run that command. Error message: " + e), user);
                Console.WriteLine("An error occurred while attempting to execute an Eco command. Error message: " + e);
            }
        }

        public static IEnumerable<StoreComponent> Stores => WorldObjectUtil.AllObjsWithComponent<StoreComponent>();


        [ChatCommand("Suggests what food you should get next.", "nextfood")]
        public static void NextFoodCmd(User user)
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                Dictionary<FoodItem, HashSet<WorldObject>> whereArtFood = new Dictionary<FoodItem, HashSet<WorldObject>>();

                IEnumerable<StoreComponent> storesWithFood = Stores
                    .Where(store => store.Enabled != false)
                    .Where(store => store.StoreData.SellOffers
                        .Where(item => item.Stack.Quantity > 0)
                        .Where(item => item.Stack.Item is FoodItem)
                        .Count() > 0);

                foreach (StoreComponent store in storesWithFood) {
                    IEnumerable<TradeOffer> foodTrades = store.StoreData.SellOffers.Where(item => item.Stack.Quantity > 0).Where(item => item.Stack.Item is FoodItem);
                    foreach (TradeOffer trade in foodTrades)
                    {
                        FoodItem foodItem = trade.Stack.Item as FoodItem;
                        if (!whereArtFood.ContainsKey(foodItem))
                        {
                            whereArtFood.Add(foodItem, new HashSet<WorldObject>());
                        }
                        whereArtFood[foodItem].Add(store.Parent);
                    }
                }

                IEnumerable<StorageComponent> enumerable = WorldObjectUtil.AllObjsWithComponent<StorageComponent>();
                IEnumerable<StorageComponent> accessibleStorage = enumerable.Where(i => i.Parent.Auth.Owners != null && i.Parent.Auth.Owners.Contains(user));

                foreach (StorageComponent storage in accessibleStorage)
                {
                    foreach (ItemStack stack in storage.Inventory.Stacks)
                    {
                        if (!(stack.Item is FoodItem))
                        {
                            continue;
                        }

                        FoodItem foodItem = stack.Item as FoodItem;
                        if (!whereArtFood.ContainsKey(foodItem))
                        {
                            whereArtFood.Add(foodItem, new HashSet<WorldObject>());
                        }
                        whereArtFood[foodItem].Add(storage.Parent);
                    }
                }

                IEnumerable<KeyValuePair<FoodItem, float>> possibleBuys = whereArtFood.Keys
                    .ToDictionary(it => it, it => FoodCalculatorComponent.getSkillPointsVariation(user.Player, it as FoodItem))
                    .Where(k => k.Value > 0)
                    .OrderByDescending(pair => pair.Value)
                    .Take(3);

                ChatManager.ServerMessageToPlayer(Localizer.DoStr(Text.Size(1.5f, Text.ColorUnity(Color.Red.UInt, "=== NextFood(NG) plugin ==="))), user);
                if (possibleBuys.Count() > 0)
                {
                    foreach (var possibleBuy in possibleBuys)
                    {
                        WorldObject store = whereArtFood[possibleBuy.Key].First();

                        LocString itemValue = LocStringExtensions.Style(
                            Localizer.DoStr(Math.Round(possibleBuy.Value, 2).ToString()),
                            Text.Styles.Positive
                        );
                        LocStringBuilder locations = new LocStringBuilder();
                        foreach (var food in whereArtFood[possibleBuy.Key])
                        {
                            if (!locations.Empty)
                            {
                                locations.Append(", ");
                            }
                            locations.Append(food.UILink());
                        }

                        ChatManager.ServerMessageToPlayer(Localizer.Format(
                            "{0} will give you {1} points and can be found at: {2}",
                            possibleBuy.Key.UILink(),
                            itemValue,
                            locations
                        ), user);
                    }
                } else
                {
                    ChatManager.ServerMessageToPlayer(Localizer.DoStr("Could not find you any food"), user);
                }
            }, user);
        }

    }
}
