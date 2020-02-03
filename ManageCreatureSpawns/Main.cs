using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using QModManager.API.ModLoading;
using Harmony;
using SettingsManager.Set;
using Newtonsoft.Json;

namespace ManageCreatureSpawns {
    [QModCore]
    public static class Qpatch
    {
      public static void Log(string logMessage, params string[] arg) {
         if (arg.Length > 0)
            logMessage = String.Format(logMessage, arg);
         Console.WriteLine("[ManageCreatureSpawns] {0}", logMessage);
      }

      public static HarmonyInstance harmony = null;
      public static Random rEngine = new Random();
      public static SettingsManager.Settings settings = null;

      public static class Manager {
         public static bool TryKillCreature(Creature creature) {
            if (creature != null && creature.enabled
               && creature.gameObject != null)
            {
               var creatureConfiguration = settings.UnwantedCreaturesList.FirstOrDefault(c =>
                  c.Name.ToLowerInvariant() == creature.name.Replace("(Clone)", String.Empty).ToLowerInvariant());
               if (creatureConfiguration != null)
               {
                  if (!creatureConfiguration.SpawnConfiguration.CanSpawn
                     || rEngine.Next(0, 100) <= creatureConfiguration.SpawnConfiguration.SpawnChance)
                  {
                     creature.tag = "Untagged";
                     creature.leashPosition = UnityEngine.Vector3.zero;

                     CreatureDeath cDeath = creature.gameObject.GetComponent<CreatureDeath>();
                     if (cDeath != null)
                     {
                        cDeath.eatable = null;
                        cDeath.respawn = false;
                        cDeath.removeCorpseAfterSeconds = 1.0f;
                     }
                     if (creature.liveMixin != null && creature.liveMixin.IsAlive())
                     {
                        if (creature.liveMixin.data != null)
                        {
                           creature.liveMixin.data.deathEffect = null;
                           creature.liveMixin.data.passDamageDataOnDeath = false;
                           creature.liveMixin.data.broadcastKillOnDeath = true;
                           creature.liveMixin.data.destroyOnDeath = true;
                           creature.liveMixin.data.explodeOnDestroy = false;
                        }
                        creature.liveMixin.Kill();
                     } else
                     {
                        creature.BroadcastMessage("OnKill");
                     }
                     return true;
                  }
               }
            }
            return false;
         }

         [HarmonyPrefix]
         [HarmonyPriority(Priority.First)]
         public static bool GenericKillCreature(Creature __instance) {
            return !TryKillCreature(__instance);
         }

         [HarmonyPrefix]
         [HarmonyPriority(Priority.First)]
         public static bool CreatureActionKillCreature(Creature __instance, ref CreatureAction __result) {
            if (TryKillCreature(__instance))
            {
               __result = null;
               return false;
            }
            return true;
         }
      }
        [QModPatch]
        public static void Patch() {
         Log("Loading... v{0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

         harmony = HarmonyInstance.Create("mod.berkay2578.managecreaturespawns");
         if (harmony != null)
         {
            Log("HarmonyInstance created.");

            Log("Reading settings.");
            {
                    JsonSerializer serializer = new JsonSerializer(typeof(SettingsManager.Settings));
               using (JsonTextReader path = new JsonTextReader("QMods\\ManageCreatureSpawns\\Settings.json"))
                  settings = (SettingsManager.Settings)serializer.Deserialize(path);
               serializer = null;

               if (settings == null)
               {
                  Log("Could not load settings, exiting.");
                  return;
               }

               foreach (var item in settings.UnwantedCreaturesList)
               {
                  Log("Loaded creature configuration: \r\n{0}", item.ToString());
               }
            }

            Log("Patching Creature events");
            {
               List<string> genericFunctionsToBePatched = new List<string>() {
                  "InitializeAgain",
                  "InitializeOnce",
                  "OnDrop",
                  "OnTakeDamage",
                  "ProcessInfection",
                  "ScanCreatureActions",
                  "Update",
                  "UpdateBehaviour",
                  "Start"
               };
               List<string> creatureActionFunctionsToBePatched = new List<string>() {
                  "ChooseBestAction",
                  "GetBestAction",
                  "GetLastAction"
               };

               foreach (string fn in genericFunctionsToBePatched)
               {
                  harmony.Patch(
                      typeof(Creature).GetMethod(fn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                      new HarmonyMethod(typeof(Manager).GetMethod("GenericKillCreature")),
                      null
                  );
                  Log("Patched Creature.{0}", fn);
               }
               foreach (string fn in creatureActionFunctionsToBePatched)
               {
                  harmony.Patch(
                      typeof(Creature).GetMethod(fn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                      new HarmonyMethod(typeof(Manager).GetMethod("CreatureActionKillCreature")),
                      null
                  );
                  Log("Patched Creature.{0}", fn);
               }
            }
            Log("Finished.");
         } else
         {
            Log("HarmonyInstance() returned null.");
         }
      }
   }
}
