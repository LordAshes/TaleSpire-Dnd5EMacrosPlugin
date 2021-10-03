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
        public const string Version = "1.5.0.0";

        public Dictionary<string, Character> characters = new Dictionary<string, Character>();

        public bool showDiagnostics = false;

        private List<Tuple<NGuid, string>> messageQueue = new List<Tuple<NGuid, string>>();
        private DateTime lastMessage = DateTime.UtcNow;
        private int messageDelay = 1000;
        private int lengthenMessage = 0;
        private int expansionFontSize = 16;

        private MessageBoard messageBoard = new MessageBoard();

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
            messageBoard.holdTime = Config.Bind("Settings", "Display Time (In Milliseconds) On Message Board", 5000).Value;
            showDiagnostics = Config.Bind("Settings", "Show Extra Diagnostics In Log", false).Value;

            CampaignSessionManager.OnStatNamesChange += StatChange;

            RadialUI.RadialUIPlugin.RemoveOnCharacter("Attacks");

            RadialUI.RadialSubmenu.EnsureMainMenuItem(DnD5EMacrosPlugin.Guid + ".Attacks", RadialUI.RadialSubmenu.MenuType.character, "Attacks", FileAccessPlugin.Image.LoadSprite("Attack.png"));
            RadialUI.RadialSubmenu.EnsureMainMenuItem(DnD5EMacrosPlugin.Guid + ".Skills", RadialUI.RadialSubmenu.MenuType.character, "Skills", FileAccessPlugin.Image.LoadSprite("Skills.png"));

            foreach (string item in FileAccessPlugin.File.Find(".Dnd5e"))
            {
                string characterName = System.IO.Path.GetFileNameWithoutExtension(item);
                if (!characters.ContainsKey(characterName))
                {
                    Debug.Log("D&D 5e Macros Plug-In: Loading Character '" + characterName + "'");
                    characters.Add(characterName, JsonConvert.DeserializeObject<Character>(FileAccessPlugin.File.ReadAllText(item)));

                    foreach (Roll roll in characters[characterName].attacks)
                    {

                        Debug.Log("D&D 5e Macros Plug-In: Adding Character '" + characterName + "' Roll '" + roll.name + "'");

                        RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                    DnD5EMacrosPlugin.Guid + ".Attacks",
                                                                    roll.name,
                                                                    FileAccessPlugin.Image.LoadSprite(roll.type + ".png"),
                                                                    (cid, obj, mi) => { AttackSelection(cid, roll); },
                                                                    true,
                                                                    () => { return CharacterCheck(characterName, roll.name); }
                                                                );

                    }

                    foreach (Roll roll in characters[characterName].skills)
                    {

                        Debug.Log("D&D 5e Macros Plug-In: Adding Character '" + characterName + "' Roll '" + roll.name + "'");

                        Sprite icon = (FileAccessPlugin.File.Exists(roll.name + ".png") ? FileAccessPlugin.Image.LoadSprite(roll.name + ".png") : FileAccessPlugin.Image.LoadSprite("Skills.png"));

                        RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                    DnD5EMacrosPlugin.Guid + ".Skills",
                                                                    roll.name,
                                                                    icon,
                                                                    (cid, obj, mi) => { SkillSelection(cid, roll); },
                                                                    true,
                                                                    () => { return CharacterCheck(characterName, roll.name); }
                                                                );

                    }
                }
                else
                {
                    Debug.LogWarning("D&D 5e Macros Plug-In: Character '" + characterName + "' Already Added.");
                }
            }

            StatMessaging.Subscribe(DnD5EMacrosPlugin.Guid, MessageBoardHandler);

            Utility.PostOnMainPage(this.GetType());
        }

        void OnGUI()
        {
            if(messageBoard.content.Count>0)
            {
                string messages = "\r\n";
                for(int m=0; m<messageBoard.content.Count; m++)
                {
                    switch((int)messageBoard.content[m].audience)
                    {
                        case (int)Audience.audience_owner: // Audience.audience_private
                            if (DateTime.UtcNow.Subtract(messageBoard.content[m].displayStart).TotalMilliseconds > messageBoard.holdTime)
                            {
                                messageBoard.content.RemoveAt(m); m--;
                            }
                            else
                            {
                                if (LocalClient.HasControlOfCreature(new CreatureGuid(messageBoard.content[m].limited)))
                                {
                                    messages = messages + messageBoard.content[m].content + "\r\n";
                                }
                            }
                            break;
                        case (int)Audience.audience_GM: // Audience.audience_secret
                            if (DateTime.UtcNow.Subtract(messageBoard.content[m].displayStart).TotalMilliseconds > messageBoard.holdTime)
                            {
                                messageBoard.content.RemoveAt(m); m--;
                            }
                            else
                            {
                                if (LocalClient.IsInGmMode)
                                {
                                    messages = messages + messageBoard.content[m].content + "\r\n";
                                }
                            }
                            break;
                        default:
                            if (DateTime.UtcNow.Subtract(messageBoard.content[m].displayStart).TotalMilliseconds > messageBoard.holdTime)
                            {
                                messageBoard.content.RemoveAt(m); m--;
                            }
                            else
                            {
                                messages = messages + messageBoard.content[m].content + "\r\n";
                            }
                            break;
                    }
                }
                int displayedLines = messages.Split('\n').Length+1;
                if (messages != "\r\n") 
                {
                    GUIStyle gs = new GUIStyle(GUI.skin.box);
                    gs.fontSize = 24;
                    gs.fontStyle = FontStyle.Bold;
                    gs.alignment = TextAnchor.MiddleCenter;
                    gs.normal.textColor = Color.white;
                    gs.normal.background = MakeColorTexture(Screen.width - 600, displayedLines * 30, new UnityEngine.Color(0.1f,0.1f,0.1f,0.75f));
                    if(GUI.Button(new Rect(300, (1080 - (displayedLines * 30)) / 2, Screen.width - 600, displayedLines * 30), messages, gs))
                    {
                        messageBoard.content.Clear();
                    }
                }
            }
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
            public string roll { get; set; } = "";
            public Roll link { get; set; } = null;
        }

        public class MessageBoard
        {
            public int holdTime { get; set; } = 2000;
            public List<Message> content { get; set; } = new List<Message>();
        }

        public class Message
        {
            public string content { get; set; } = "";
            public Audience audience { get; set; } = Audience.audience_owner;
            public DateTime displayStart { get; set; } = DateTime.UtcNow;
            public string limited { get; set; } = "";
        }

        public enum Audience
        {
            audience_public = 0,
            audience_owner = 1,
            audience_private = 1,
            audience_GM = 2,
            audience_secret = 2
        }
    }
}