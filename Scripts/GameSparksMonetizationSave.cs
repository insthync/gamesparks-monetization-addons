using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSparks;
using GameSparks.Api;
using GameSparks.Api.Messages;
using GameSparks.Api.Requests;
using GameSparks.Api.Responses;
using GameSparks.Core;

public class GameSparksMonetizationSave : BaseMonetizationSave
{
    public static readonly Dictionary<string, int> CurrencyAmounts = new Dictionary<string, int>();
    public static PurchasedItems Items = new PurchasedItems();
    private void Awake()
    {
        RequestCurrencyData();
    }

    public void RequestCurrencyData()
    {
        var currencyKeys = new List<string>(MonetizationManager.Currencies.Keys);
        var currencyAmounts = new int[6];
        new AccountDetailsRequest().Send((response) => {

            currencyAmounts[0] = (int)response.Currency1;
            currencyAmounts[1] = (int)response.Currency2;
            currencyAmounts[2] = (int)response.Currency3;
            currencyAmounts[3] = (int)response.Currency4;
            currencyAmounts[4] = (int)response.Currency5;
            currencyAmounts[5] = (int)response.Currency6;
            var i = 0;
            while (i < currencyKeys.Count && i < 6)
            {
                CurrencyAmounts[currencyKeys[i]] = currencyAmounts[i];
                ++i;
            }
        });
    }

    public override bool AddCurrency(string name, int amount)
    {
        if (amount == 0)
            return true;
        if (!CurrencyAmounts.ContainsKey(name))
            return false;
        var newAmount = CurrencyAmounts[name] + amount;
        if (newAmount < 0)
            return false;
        SetCurrency(name, newAmount);
        return true;
    }

    public override void AddPurchasedItem(string itemName)
    {
        Items.Add(itemName);
        SetPurchasedItems(Items);
    }

    public override int GetCurrency(string name)
    {
        return CurrencyAmounts[name];
    }

    public override PurchasedItems GetPurchasedItems()
    {
        return Items;
    }

    public override void SetCurrency(string name, int amount)
    {
        var keys = new List<string>(CurrencyAmounts.Keys);
        new LogEventRequest().SetEventKey("SERVICE_EVENT")
            .SetEventAttribute("target", "setCurrency")
            .SetEventAttribute("currencyIndex", keys.IndexOf(name))
            .SetEventAttribute("currencyAmount", amount)
            .Send((response) =>
            {
                if (!response.HasErrors)
                    CurrencyAmounts[name] = amount;
            });
    }

    public override void SetPurchasedItems(PurchasedItems purchasedItems)
    {
        if (purchasedItems == null)
            return;

        var json = "[";
        foreach (var itemName in Items.itemNames)
        {
            if (json.Length > 1)
                json += ",";
            json += "\"" + itemName + "\"";
        }
        json += "]";
        new LogEventRequest().SetEventKey("SERVICE_EVENT")
            .SetEventAttribute("target", "setPurchasedItems")
            .SetEventAttribute("items", json)
            .Send((response) =>
            {
                if (!response.HasErrors)
                    Items = purchasedItems;
            });
    }
}
