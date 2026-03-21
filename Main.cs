using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace ModBase
{
    public class Main
    {
        public static bool enabled { get; private set; }

        /// <summary>This is provided by UnityModManager to log messages to the console</summary>
        public static ModEntry.ModLogger Logger;
        /// <summary>Main access to this mod's settings</summary>
        public static Settings settings;
        /// <summary>This will be called when the mod is toggles on/off</summary>
        public static event Action<bool> OnToggle;

        static List<GameObject> markers;
        static Material markerMat;

        // Called by the mod manager
        static bool Load(ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            settings = ModSettings.Load<Settings>(modEntry);

            // Harmony patching
            Harmony harmony = new Harmony(modEntry.Info.Id);
            Main.Try("Harmony patching", () => harmony.PatchAll(), true);

            // hook in mod manager event
            modEntry.OnToggle = OnToggleEvent;
            modEntry.OnGUI = (entry) =>
            {
                settings.Draw(entry);
                settings.OnGUI();
            };
            modEntry.OnSaveGUI = settings.Save;

            markers = new List<GameObject>();
            return true;
        }

        static bool OnToggleEvent(ModEntry modEntry, bool state)
        {
            enabled = state;
            OnToggle?.Invoke(state);
            return true;
        }

        /// <summary>Logs a message to the console</summary>
        public static void Log(string message)
        {
            if (!settings.disableInfoLogs)
                Logger.Log(message);
        }

        /// <summary>Logs an error message to the console</summary>
        public static void Error(string message) => Logger.Error(message);

        /// <summary>Use this to log possible execution errors to the console</summary>
        public static void Try(string flag, Action callback, bool crashOnError = false)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Error(flag + "\n" + e.ToString());

                if(crashOnError)
                    throw e;
            }
        }

        /// <summary>BindingFlags.NonPrivate is implicit / source can be null</summary>
        public static T GetField<T, U>(U source, string fieldName, BindingFlags flags)
        {
            FieldInfo info = typeof(U).GetField(fieldName, flags | BindingFlags.NonPublic);

            if (info == null)
            {
                Error("Couldn't find field info for field \"" + fieldName + "\" in type \"" + source.GetType() + "\"");
                return default(T);
            }

            return (T)info.GetValue(source);
        }

        /// <summary>BindingFlags.NonPrivate is implicit / source can be null</summary>
        public static void SetField<T>(T source, string fieldName, BindingFlags flags, object value)
        {
            FieldInfo info = typeof(T).GetField(fieldName, flags | BindingFlags.NonPublic);

            if (info == null)
            {
                Error("Couldn't find field info for field \"" + fieldName + "\" in type \"" + source.GetType() + "\"");
                return;
            }

            info.SetValue(source, value);
        }

        /// <summary>BindingFlags.NonPublic is implicit / source can be null</summary>
        public static void InvokeMethod<T>(T source, string methodName, BindingFlags flags, object[] args)
        {
            MethodInfo info = typeof(T).GetMethod(methodName, flags | BindingFlags.NonPublic);

            if (info == null)
            {
                Error("Couldn't find method info for method \"" + methodName + "\" in type \"" + source.GetType() + "\"");
                return;
            }

            info.Invoke(source, args);
        }

        /// <summary>BindingFlags.NonPrivate is implicit / source can be null</summary>
        public static U InvokeMethod<T, U>(T source, string methodName, BindingFlags flags, object[] args)
        {
            MethodInfo info = typeof(T).GetMethod(methodName, flags | BindingFlags.NonPublic);

            if (info == null)
            {
                Error("Couldn't find method info for method \"" + methodName + "\" in type \"" + source.GetType() + "\"");
                return default;
            }

            return (U)info.Invoke(source, args);
        }

        public static void SetMarkers(bool state)
        {
            CleanMarkerList();
            markers.ForEach(item => item.SetActive(state));
        }

        public static void AddMarker(Transform parent, Vector3 position, float size)
        {
            if (markerMat == null)
            {
                markerMat = new Material(Shader.Find("Standard"));
                markerMat.color = Color.red;
                markerMat.SetColor("_EmissionColor", Color.red);
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.GetComponent<Renderer>().material = markerMat;

            marker.transform.SetParent(parent);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * size;

            marker.SetActive(settings.showMarkers);
            markers.Add(marker);
        }

        static void CleanMarkerList()
        {
            List<int> toRemove = new List<int>();

            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] == null)
                    toRemove.Add(i);
            }

            toRemove.Reverse();
            toRemove.ForEach(index => markers.RemoveAt(index));
        }
    }
}
