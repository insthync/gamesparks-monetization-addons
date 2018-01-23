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
    public static bool IsRequestedCurrencyOnce { get; private set; }
    public static bool IsRequestedItemsOnce { get; private set; }
    public static bool IsRequestingCurrencyService { get; private set; }
    public static bool IsRequestingItemsService { get; private set; }
    private void Awake()
    {
        IsRequestedCurrencyOnce = false;
        IsRequestedItemsOnce = false;
        IsRequestingCurrencyService = false;
        IsRequestingItemsService = false;
        RequestCurrencyData();
    }

    public void RequestCurrencyData()
    {
        if (IsRequestingCurrencyService)
            return;
        IsRequestingCurrencyService = true;
        
        new AccountDetailsRequest().Send((response) => {
            var currencies = response.Currencies.BaseData;
            foreach (var currency in currencies)
            {
                CurrencyAmounts[currency.Key] = System.Convert.ToInt32(currency.Value);
            }
            IsRequestedCurrencyOnce = true;
            IsRequestingCurrencyService = false;
        });
    }

    public void RequestItemData()
    {
        if (IsRequestingItemsService)
            return;
        IsRequestingItemsService = true;

        var keys = new List<string>(CurrencyAmounts.Keys);
        new LogEventRequest().SetEventKey("SERVICE_EVENT")
            .SetEventAttribute("TARGET", "getPurchasedItems")
            .Send((response) =>
            {
                IsRequestedItemsOnce = true;
                IsRequestingItemsService = false;
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
        if (!IsRequestedCurrencyOnce)
            return;

        if (!CurrencyAmounts.ContainsKey(name) || CurrencyAmounts[name] == amount)
            return;

        if (IsRequestingCurrencyService)
            return;
        IsRequestingCurrencyService = true;

        var keys = new List<string>(CurrencyAmounts.Keys);
        var json = string.Format("{ \"currencyId\" : \"{0}\", \"currencyAmount\" : \"{1}\" }", name, amount);
        new LogEventRequest().SetEventKey("SERVICE_EVENT")
            .SetEventAttribute("TARGET", "setCurrency")
            .SetEventAttribute("DATA", new GSRequestData(json))
            .Send((response) =>
            {
                if (!response.HasErrors)
                    CurrencyAmounts[name] = amount;

                IsRequestingCurrencyService = false;
            });
    }

    public override void SetPurchasedItems(PurchasedItems purchasedItems)
    {
        if (!IsRequestedItemsOnce)
            return;

        if (purchasedItems == null)
            return;

        if (IsRequestingItemsService)
            return;
        IsRequestingItemsService = true;

        var items = "";
        foreach (var itemName in Items.itemNames)
        {
            if (items.Length > 1)
                items += ",";
            items += "\"" + itemName + "\"";
        }
        var json = string.Format("{ \"items\" : [{0}] }", items);
        new LogEventRequest().SetEventKey("SERVICE_EVENT")
            .SetEventAttribute("TARGET", "setPurchasedItems")
            .SetEventAttribute("DATA", new GSRequestData(items))
            .Send((response) =>
            {
                if (!response.HasErrors)
                    Items = purchasedItems;

                IsRequestingItemsService = false;
            });
    }
}
