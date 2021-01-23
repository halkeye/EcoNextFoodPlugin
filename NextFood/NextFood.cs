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
    public class NextFoodChatCommand : IChatCommandHandler
    {
        public NextFoodChatCommand()
        {
        }

        /// <inheritdoc />
        public string GetStatus()
        {
            return "Active";
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
                ChatManager.ServerMessageToPlayer(Localizer.Format("Error occurred while attempting to run that command. Error message: {0}", e), user);
                Console.WriteLine("An error occurred while attempting to execute an Eco command. Error message: " + e);
            }
        }

        public static IEnumerable<StoreComponent> Stores => WorldObjectUtil.AllObjsWithComponent<StoreComponent>();

        private static void addItem(Dictionary<FoodItem, HashSet<LocString>> whereArtFood, FoodItem foodItem, LocString uiLink) {
            if (!whereArtFood.ContainsKey(foodItem))
            {
                whereArtFood.Add(foodItem, new HashSet<LocString>());
            }
            whereArtFood[foodItem].Add(uiLink);
        }

        public static LocString NextFoodBody(User user, String count = "3")
        {
            Dictionary<FoodItem, HashSet<LocString>> whereArtFood = new Dictionary<FoodItem, HashSet<LocString>>();

            if (user.Carrying != null && user.Carrying.Item is FoodItem)
            {
                addItem(whereArtFood, user.Carrying.Item as FoodItem, user.UILink());
            }

            foreach (ItemStack stack in user.Inventory.ToolbarBackpack.Stacks.Where(item => item.Item is FoodItem))
            {
                addItem(whereArtFood, stack.Item as FoodItem, user.UILink());
            }

            IEnumerable<StorageComponent> enumerable = WorldObjectUtil.AllObjsWithComponent<StorageComponent>();
            IEnumerable<StorageComponent> accessibleStorage = enumerable.Where(i => i.Parent.Auth.Owners != null && i.Parent.Auth.Owners.Contains(user));

            foreach (StorageComponent storage in accessibleStorage)
            {
                foreach (ItemStack stack in storage.Inventory.Stacks.Where(item => item.Item is FoodItem))
                {
                    addItem(whereArtFood, stack.Item as FoodItem, storage.Parent.UILink());
                }
            }

            IEnumerable<StoreComponent> storesWithFood = Stores
                .Where(store => store.Enabled != false)
                .Where(store => store.StoreData.SellOffers
                    .Where(item => item.Stack.Quantity > 0)
                    .Where(item => item.Stack.Item is FoodItem)
                    .Count() > 0);

            foreach (StoreComponent store in storesWithFood)
            {
                IEnumerable<TradeOffer> foodTrades = store.StoreData.SellOffers.Where(item => item.Stack.Quantity > 0).Where(item => item.Stack.Item is FoodItem);
                foreach (TradeOffer trade in foodTrades)
                {
                    addItem(whereArtFood, trade.Stack.Item as FoodItem, store.Parent.UILink());
                }
            }

            IEnumerable<KeyValuePair<FoodItem, float>> possibleBuys = whereArtFood.Keys
                .ToDictionary(it => it, it => FoodCalculatorComponent.getSkillPointsVariation(user.Player, it as FoodItem))
                .Where(it => it.Key.Calories != 0)
                .OrderByDescending(pair => pair.Value)
                .Take(Int32.Parse(count));

            List<LocString> body = new List<LocString>(); 
            if (possibleBuys.Count() > 0)
            {
                foreach (var possibleBuy in possibleBuys)
                {
                    LocString itemValue = LocStringExtensions.Style(
                        Localizer.DoStr(Math.Round(possibleBuy.Value, 2).ToString()),
                        possibleBuy.Value >= 0 ? Text.Styles.Positive : Text.Styles.Negative
                    );
                    String locations = String.Join(", ", whereArtFood[possibleBuy.Key]);
                    body.Add(Localizer.Format("{0} will give you {1} points and can be found at: {2}", possibleBuy.Key.UILink(), itemValue, locations));
                }
            }
            else
            {
                body.Add(Localizer.DoStr("Could not find you any food"));
            }
            return Localizer.DoStr(String.Join("\n", body));
        }

        [ChatCommand("Suggests what food you should get next.", "")]
        public static void NextFood(User user, String count = "3")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                LocString nextFoodBody = NextFoodBody(lUser, args[0]);
                ChatManager.ServerMessageToPlayer(Localizer.DoStr(Text.Size(1.5f, Text.ColorUnity(Color.Red.UInt, "=== NextFood plugin ==="))), lUser);
                ChatManager.ServerMessageToPlayer(nextFoodBody, lUser);
            }, user, count);
        }

/*
        [ChatCommand("Suggests what food you should get next.", "")]
        public static void NextFood1(User user, String count = "3")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                LocString nextFoodBody = NextFoodBody(lUser, args[0]);
                ChatManager.ServerMessageToPlayer(Localizer.DoStr(Text.Size(1.5f, Text.ColorUnity(Color.Red.UInt, "=== NextFood plugin ==="))), lUser);
                ChatManager.ServerMessageToPlayer(nextFoodBody, lUser);
            }, user, count);
        }

        [ChatCommand("Suggests what food you should get next.", "")]
        public static void NextFood2(User user, String count = "3")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                LocString nextFoodBody = NextFoodBody(lUser, args[0]);
                user.Player.InfoBoxLocStr(
                    Localizer.DoStr(
                        Text.Size(1.5f, Text.ColorUnity(Color.Red.UInt, "=== NextFood plugin ===")) + "\n" +
                        nextFoodBody
                     )
                 );
            }, user, count);
        }


        [ChatCommand("Suggests what food you should get next.", "")]
        public static void NextFood3(User user, String count = "3")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                LocString nextFoodBody = NextFoodBody(lUser, args[0]);
                lUser.Player.OkBox(
                    Localizer.DoStr(
                        Text.Size(1.5f, Text.ColorUnity(Color.Red.UInt, "=== NextFood plugin ===")) + "\n" +
                        nextFoodBody
                     )
                 );
            }, user, count);
        }

        [ChatCommand("Suggests what food you should get next.", "")]
        public static void NextFood4(User user, String count = "3")
        {
            CallWithErrorHandling<object>((lUser, args) =>
            {
                LocString nextFoodBody = NextFoodBody(lUser, args[0]);
                lUser.Player.OpenInfoPanel(Localizer.DoStr("What should you eat next?"), nextFoodBody, "");
            }, user, count);
        }*/
    }
}
