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

            IEnumerable<StorageComponent> accessibleStorage = WorldObjectUtil.AllObjsWithComponent<StorageComponent>().Where(i => i.Parent.Auth.UsersWithConsumerAccess.Contains(user));

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

            CallWithErrorHandling<object>((lUser, args) =>
            {
                // Item.Get<Item>().UILink() || this.UILink()<-- this is used in the recipes of items

                List<LocString> locationStrs = new List<LocString>();
                foreach (var possibleBuy in possibleBuys) {
                    WorldObject store = whereArtFood[possibleBuy.Key].First();
                    
                    LocString itemValue = LocStringExtensions.Style(
                        Localizer.DoStr(Math.Round(possibleBuy.Value, 2).ToString()),
                        Text.Styles.Positive
                    );
                    // string itemValue = Text.ColorUnity(Color.Green.UInt, Text.Size(1.3f, "+ " + Math.Round(possibleBuy.Value, 2))));

                    string locations = String.Join(", ", whereArtFood[possibleBuy.Key].Select(food => food.UILinkContent()));
                    locationStrs.Add(Localizer.Format(
                        "{0} will give you {1} points and can be found at: {2}",
                        possibleBuy.Key.UILinkContent(),
                        itemValue,
                        locations
                    ));
                }

                ChatManager.ServerMessageToPlayer(Localizer.DoStr(Text.Size(1.5f, Text.ColorUnity(Color.Red.UInt, "=== NextFood plugin ==="))), user);
                locationStrs.ForEach(m => ChatManager.ServerMessageToPlayer(m, user));
                
            }, user);
            /* try
             {
                 foodTradesItems = trades
                     .Where(t => t.NumberAvailable > 0)
                     .Select(t => Item.Get(t.ItemTypeID))
                     .Where(item => item != null && item is FoodItem)
                     .Where(item =>
                     {
                         try
                         {
                             if (item.DisplayName.NotTranslated != null)
                                 return true;
                         }
                         catch (Exception e)
                         {
                             return false;
                         }

                         return false;
                     }).ToList()
                     .DistinctBy(item => item.DisplayName.NotTranslated)
                     .ToList();
             }
             catch (Exception e)
             {
                 NextFood.printMessage("Error while get trades");
                 NextFood.printMessage(e.Message);
                 NextFood.printMessage(e.StackTrace);
                 NextFood.printMessage(e.GetType().Name);
             }

             if (foodTradesItems == null)
                 return;


             var possibleBuy = foodTradesItems
                 .ToDictionary(it => it, it => FoodCalculatorComponent.getSkillPointsVariation(__instance.Owner.Player, it as FoodItem))
                 .Where(k => k.Value > 0)
                 .OrderByDescending(pair => pair.Value).Take(3);

             string subTittle = possibleBuy.Any() ? "This food on the market will increase your skpts:" : "There is no interesting food on the market";
             subTittle = Text.ColorUnity(Color.Yellow.UInt, subTittle);

             List<string> addedString = new List<string>()
                {
                    "",
                    "",
                    Text.Size(2f,Text.ColorUnity(Color.Red.UInt,"=== NextFood plugin ===")),
                    subTittle,
                    "",
                };

             possibleBuy.ForEach(k =>
             {
                 string itemValue = Text.Size(1.3f, "+ " + Math.Round(k.Value, 2));
                 itemValue = Text.Bold(itemValue);
                 itemValue = Text.ColorUnity(Color.Green.UInt, itemValue);
                 addedString.Add($"- {k.Key.DisplayName.NotTranslated} for {itemValue} skpts /day");
             });

             __result += string.Join("\n", addedString);
         }
         catch (Exception e)
         {
             NextFood.printMessage(e.Message);
             NextFood.printMessage(e.StackTrace);
             e.PrettyPrint();
         }
*/
        }

    }
}
