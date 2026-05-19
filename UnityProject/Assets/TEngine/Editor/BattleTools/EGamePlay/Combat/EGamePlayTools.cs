using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System;

namespace EGamePlay
{
    public static class EGamePlayTools
    {
        //[MenuItem("Tools/EGamePlay/使用技能Excel配置")]
        public static void UseExcel()
        {
            var dedfine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            var defineStr = $"{dedfine};EGAMEPLAY_EXCEL";
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, defineStr);
        }

        //[MenuItem("Tools/EGamePlay/使用技能Excel配置", true)]
        public static bool IsUseExcel()
        {
            var dedfine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            return !dedfine.Contains("EGAMEPLAY_EXCEL");
        }

        //[MenuItem("Tools/EGamePlay/使用技能ScriptableObj配置")]
        public static void UseScripableObj()
        {
            var dedfine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            dedfine = dedfine.Replace(";EGAMEPLAY_EXCEL", "");
            dedfine = dedfine.Replace("EGAMEPLAY_EXCEL;", "");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, dedfine);
        }

        //[MenuItem("Tools/EGamePlay/使用技能ScriptableObj配置", true)]
        public static bool IsUseScripableObj()
        {
            var dedfine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            return dedfine.Contains("EGAMEPLAY_EXCEL");
        }
    }
}