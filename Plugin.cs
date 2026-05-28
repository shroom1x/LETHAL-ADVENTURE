using BepInEx;
using HarmonyLib;
using System;

[BepInPlugin("com.shroom1x.LAchangeloger", "LA_Changeloger", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony("com.shroom1x.LAchangeloger");

    void Awake()
    {
        harmony.PatchAll();
        Logger.LogInfo("LA_Changeloger loaded and patches applied!");
    }
}
