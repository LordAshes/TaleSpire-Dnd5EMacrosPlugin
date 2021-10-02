using BepInEx;
using BepInEx.Configuration;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.StatMessaging.Guid)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    public partial class DnD5EMacrosPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Dnd5e Macros Plug-In";
        public const string Guid = "org.lordashes.plugins.dnd5emacros";
        public const string Version = "1.0.0.0";

        public Dictionary<string, Character> characters = new Dictionary<string, Character>();

        private List<Tuple<NGuid, string>> messageQueue = new List<Tuple<NGuid, string>>();
        private DateTime lastMessage = DateTime.UtcNow;
        private int messageDelay = 1000;
        private int lengthenMessage = 0;
        private int expansionFontSize = 16;

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("DnD5e Macros Plugin: Active.");

            messageDelay = Config.Bind("Settings", "Message Delay", 1000).Value;
            lengthenMessage = Config.Bind("Settings", "Increase Message Duration", 0).Value;
            expansionFontSize = Config.Bind("Settings", "Chat Expension Font Size", 16).Value;

            CampaignSessionManager.OnStatNamesChange += StatChange;

            RadialUI.RadialUIPlugin.RemoveOnCharacter("Attacks");

            RadialUI.RadialSubmenu.EnsureMainMenuItem(DnD5EMacrosPlugin.Guid + ".Attacks", RadialUI.RadialSubmenu.MenuType.character, "Attacks", FileAccessPlugin.Image.LoadSprite("Attack.png"));

            foreach (string item in FileAccessPlugin.File.Find(".Dnd5e"))
            {
                string characterName = System.IO.Path.GetFileNameWithoutExtension(item);
                Debug.Log("D&D 5e Macros Plug-In: Loading Character '" + characterName + "'");
                characters.Add(characterName,JsonConvert.DeserializeObject<Character>(FileAccessPlugin.File.ReadAllText(item)));

                foreach(Roll roll in characters[characterName].attacks)
                {

                    Debug.Log("D&D 5e Macros Plug-In: Adding Character '" + characterName + "' Roll '" + roll.name + "'");

                    RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                DnD5EMacrosPlugin.Guid + ".Attacks",
                                                                roll.name,
                                                                FileAccessPlugin.Image.LoadSprite(roll.type+".png"),
                                                                (cid, obj, mi) => { MenuSelection(cid, roll); },
                                                                true,
                                                                () => { return CharacterCheck(characterName, roll.name); }
                                                            );

                }
            }

            Utility.PostOnMainPage(this.GetType());
        }

        void Update()
        {
            if(messageQueue.Count>0)
            {
                if(DateTime.UtcNow.Subtract(lastMessage).TotalMilliseconds>messageDelay)
                {
                    lastMessage = DateTime.UtcNow;
                    Tuple<NGuid, string> message = messageQueue[0];
                    messageQueue.RemoveAt(0);
                    ChatManager.SendChatMessage(AddEnvelope(message.Item2, lengthenMessage), message.Item1);
                }
            }
        }

        public class Character
        {
            public bool NPC { get; set; } = false;
            public List<Roll> attacks { get; set; } = new List<Roll>();
            public List<Roll> skills { get; set; } = new List<Roll>();
            public List<string> resistance { get; set; } = new List<string>();
            public List<string> immunity { get; set; } = new List<string>();
        }

        public class Roll
        {
            public string name { get; set; } = "";
            public string type { get; set; } = "";
            public string roll { get; set; } = "1D20";
            public Roll link { get; set; } = null;
        }
    }
}