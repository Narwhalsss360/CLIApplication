﻿using CLIApplication;
using System.ComponentModel;


Dictionary<string, string> userSavedItems = new();

CLIInterpreter interpreter = new(SaveItem, DeleteItem, ShowItem, Quit, q) { EntryMarker = "→", FlagMarker = "~~", InterfaceName = "CLI App Test" };

Type t = typeof(MyEnum);

[DisplayName("save-item")]
void SaveItem(string key, string? value = null)
{
    if (!userSavedItems.ContainsKey(key))
        userSavedItems.Add(key, "");
    userSavedItems[key] = value is not null ? value : "";
    ShowItem(key);
}

void DeleteItem(string key)
{
    if (!userSavedItems.ContainsKey(key))
    {
        Console.WriteLine($"Key {key} does not exist!");
        return;
    }

    ShowItem(key);
    userSavedItems.Remove(key);
}

void ShowItem(string? key = null)
{
    if (key is null)
    {
        foreach (var item in userSavedItems.Keys)
            ShowItem(key);
        return;
    }

    if (!userSavedItems.ContainsKey(key))
    {
        Console.WriteLine($"Key {key} does not exist!");
        return;
    }

    Console.WriteLine($"{key}: {userSavedItems[key]}");
}

void Quit(string[]? flags = null, CLIInterpreter? sender = null)
{
    if (sender is null)
    {
        Environment.Exit(-30);
        return;
    }

    sender.StopRunExecution = true;
}

void q(string[]? flags = null, CLIInterpreter? sender = null) => Quit();
