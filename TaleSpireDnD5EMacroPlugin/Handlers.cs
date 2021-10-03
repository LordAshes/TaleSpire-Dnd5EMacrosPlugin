using BepInEx;
using Bounce.Unmanaged;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LordAshes
{
    public partial class DnD5EMacrosPlugin : BaseUnityPlugin
    {
        private System.Random random = new System.Random();
        private int statForAC = 0;

        private void StatChange(string[] names)
        {
            for(int i=0; i<names.Length; i++)
            {
                if (names[i] == "AC") { statForAC = i; Debug.Log("D&D 5e Macros Plug-In: Found AC at Index " + i); }
            }
        }

        private void MessageBoardHandler(StatMessaging.Change[] messages)
        {
            foreach(StatMessaging.Change message in messages)
            {
                if (message.action != StatMessaging.ChangeType.removed)
                {
                    if (message.value != "")
                    {
                        messageBoard.content.Add(JsonConvert.DeserializeObject<Message>(message.value));
                    }
                }
            }
        }

        private bool CharacterCheck(string characterName, string rollName)
        {
            CreatureBoardAsset asset = null;
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
            if (asset == null) { return false; }
            return (StatMessaging.GetCreatureName(asset) == characterName);
        }

        private void AttackSelection(CreatureGuid cid, Roll roll)
        {
            if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Attack Option '"+roll.name+"' Selected"); }
            CreatureBoardAsset attacker = null;
            CreatureBoardAsset victim = null;
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out attacker);
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out victim);

            if(attacker!=null && victim!=null)
            {
                RollResult attackRoll = ExecuteRoll(roll.roll);
                if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Attack Roll = "+JsonConvert.SerializeObject(attackRoll)); }
                int victimAC = (int)victim.Creature.GetStatByIndex(statForAC).Max;
                if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Victim AC = " + victimAC); }
                string attackMessage = roll.name + " " + attackRoll.total + "\r\n(" + attackRoll.expanded+")";
                if (attackRoll.minRoll) { attackMessage = attackMessage + "\r\n(Critical Miss)"; }
                if (attackRoll.maxRoll) { attackMessage = attackMessage + "\r\n(Critical Hit)"; }
                if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Queue Message = " + attackMessage); }
                messageQueue.Add(new Tuple<NGuid,string>(new NGuid(attacker.Creature.CreatureId.ToString()),attackMessage));
                if ((attackRoll.total>=victimAC && !attackRoll.minRoll) || attackRoll.maxRoll)
                {
                    if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Hit"); }
                    CreatureManager.AttackCreature(attacker.Creature.CreatureId, attacker.transform.position, victim.Creature.CreatureId, victim.transform.position);
                    if (!characters[StatMessaging.GetCreatureName(victim)].NPC)
                    {
                        if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: NPC victim"); }
                        if (attackRoll.maxRoll)
                        {
                            if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Critical Hit"); }
                            if (characters[StatMessaging.GetCreatureName(victim)].immunity.Contains("Criticals"))
                            {
                                messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Critical Hit!\r\n(Immunity)\r\n(AC:" + victimAC + ")"));
                                attackRoll.maxRoll = false;
                            }
                            else
                            {
                                messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Critical Hit!\r\n(AC:" + victimAC + ")"));
                            }
                        }
                        else
                        {
                            if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Regular Hit"); }
                            messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Hit!\r\n(AC:" + victimAC + ")"));
                        }
                    } 
                    else 
                    {
                        if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: PC victim"); }
                        if (attackRoll.maxRoll)
                        {
                            if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Critical Hit"); }
                            if (characters[StatMessaging.GetCreatureName(victim)].immunity.Contains("Criticals"))
                            {
                                messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Critical Hit!\r\n(Immunity)"));
                                attackRoll.maxRoll = false;
                            }
                            else
                            {
                                messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Critical Hit!"));
                            }
                        }
                        else
                        {
                            if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Regular Hit"); }
                            messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Hit!"));
                        }
                    }
                    if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Determining Damage"); }
                    RollResult attackerDamage = new RollResult();
                    RollResult attackerDamageChat = new RollResult();
                    RollResult victimDamage = new RollResult();
                    RollResult victimDamageChat = new RollResult();
                    while (roll.link!=null)
                    {
                        roll = roll.link;
                        if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Processing Damage '"+roll.name+"'"); }
                        RollResult thisDamageRoll = ExecuteRoll(roll.roll, attackRoll.maxRoll);

                        attackerDamage.total = attackerDamage.total + thisDamageRoll.total;
                        attackerDamage.expanded = (attackerDamage.expanded == "") ? roll.name + ": " + thisDamageRoll.total + " " + roll.type : attackerDamage.expanded + "\r\n" + roll.name + ": " + thisDamageRoll.total + " " + roll.type;
                        attackerDamageChat.expanded = (attackerDamageChat.expanded == "") ? thisDamageRoll.expanded : attackerDamageChat.expanded + ", " + thisDamageRoll.expanded;

                        if (characters[StatMessaging.GetCreatureName(victim)].immunity.Contains(roll.type)) { thisDamageRoll.total = 0; thisDamageRoll.expanded = "("+thisDamageRoll.expanded+")x0"; }
                        if (characters[StatMessaging.GetCreatureName(victim)].resistance.Contains(roll.type)) { thisDamageRoll.total = (int)Math.Ceiling((decimal)(thisDamageRoll.total / 2)); thisDamageRoll.expanded = "("+thisDamageRoll.expanded + ")x0.5"; }

                        victimDamage.total = victimDamage.total + thisDamageRoll.total;
                        victimDamage.expanded = (victimDamage.expanded == "") ? roll.name + ": " + thisDamageRoll.total + " " + roll.type  : victimDamage.expanded + "\r\n" + roll.name + ": " + thisDamageRoll.total + " " + roll.type;
                        victimDamageChat.expanded = (victimDamageChat.expanded == "") ? thisDamageRoll.expanded : victimDamageChat.expanded + ", " + thisDamageRoll.expanded;
                    }
                    if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Queuing Inflict Messages"); }
                    messageQueue.Add(new Tuple<NGuid, string>(new NGuid(attacker.Creature.CreatureId.ToString()), "Inflicts " + attackerDamage.total+" Damage\r\n\r\n"+attackerDamage.expanded));
                    messageQueue.Add(new Tuple<NGuid, string>(NGuid.Empty, "<size="+expansionFontSize+">"+attackerDamageChat.expanded+"</size>"));
                    if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Adjusting Victim HP"); }
                    int newHP = (int)(victim.Creature.Hp.Value - victimDamage.total);
                    newHP = Math.Max(0, newHP);
                    CreatureManager.SetCreatureStatByIndex(victim.Creature.CreatureId, new CreatureStat(newHP,victim.Creature.Hp.Max), -1);
                    if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Queuing Received Messages"); }
                    messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Receives "+victimDamage.total+" Damage\r\n\r\n"+victimDamage.expanded));
                    messageQueue.Add(new Tuple<NGuid, string>(NGuid.Empty, "<size="+expansionFontSize+">"+victimDamageChat.expanded+"</size>"));
                    if (!characters[StatMessaging.GetCreatureName(victim)].NPC)
                    {
                        if (newHP > 0)
                        {
                            messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "At " + newHP + " of " + victim.Creature.Hp.Max + " HP"));
                        }
                        else
                        {
                            messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Unconcious and dying"));
                        }
                    }
                }
                else
                {
                    if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Miss"); }
                    victim.Creature.PlayEmote("");
                    if (!characters[StatMessaging.GetCreatureName(victim)].NPC)
                    {
                        if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: NPC"); }
                        messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Miss!\r\n(AC:" + victimAC + ")"));
                    }
                    else 
                    {
                        if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: PC"); }
                        messageQueue.Add(new Tuple<NGuid, string>(new NGuid(victim.Creature.CreatureId.ToString()), "Miss!"));
                    }
                }
            }
            else
            {
                Debug.LogWarning("Attack Selection Made With Attacker Or Victim Being Null. Attacker: " + Convert.ToString(attacker) + ". Victim: " + Convert.ToString(victim));
            }
        }

        private void SkillSelection(CreatureGuid cid, Roll roll)
        {
            if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Skill Selection '"+roll.name+"'"); }
            CreatureBoardAsset asset = null;
            CreaturePresenter.TryGetAsset(cid, out asset);
            if (asset == null) { return; }
            RollResult result = default(RollResult);
            while (roll != null)
            {
                if (showDiagnostics) { Debug.Log("D&D 5E Macros Plugin: Processing '" + roll.name + "' (Type: "+roll.type+")"); }
                
                string content = "";
                switch (roll.type.ToUpper())
                {
                    case "GM":
                    case "SECRET":
                    case "OWNER":
                    case "PRIVATE":
                        if (roll.roll != "")
                        {
                            result = ExecuteRoll(roll.roll);
                            content = roll.name + " " + result.total + "\r\n(" + result.expanded + ")";
                            if (result.minRoll) { content = content + "\r\n[Min Roll]"; }
                            if (result.minRoll) { content = content + "\r\n[Max Roll]"; }
                        }
                        else
                        {
                            content = roll.name;
                        }
                        MessageBoardPost(content, roll.type.ToUpper(), cid);
                        break;
                    case "OPPOSED":
                        RollResult DC = result;
                        MessageBoardPost(" ", Audience.audience_secret, asset.Creature.CreatureId);
                        if (LocalClient.SelectedCreatureId.ToString() == cid.ToString())
                        {
                            if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Cycling Through Other Characters To Opposed Roll"); }
                            foreach (KeyValuePair<string, Character> target in characters)
                            {
                                if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Found " + target.Key); }
                                if (target.Key != StatMessaging.GetCreatureName(asset))
                                {
                                    if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Seeking '" + roll.name + "'"); }
                                    foreach (Roll skill in target.Value.skills)
                                    {
                                        if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Found '" + skill.name + "'"); }
                                        if (skill.name == roll.name)
                                        {
                                            if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Rolling '" + skill.name + "'"); }
                                            result = ExecuteRoll(skill.roll);
                                            if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Queuing Rolling '" + target.Key + ": " + roll.name + ": " + result.total + "'"); }
                                            MessageBoardPost(target.Key + ": " + roll.name + ": " + result.total + ": " + ((result.total >= DC.total) ? "Opposed" : "Unopposed"), Audience.audience_secret, asset.Creature.CreatureId);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            CreatureBoardAsset opposer;
                            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out opposer);
                            if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Found " + StatMessaging.GetCreatureName(opposer) + ", Seeking Skill '" + roll.name + "'"); }
                            Character target = characters[StatMessaging.GetCreatureName(opposer)];
                            foreach (Roll skill in target.skills)
                            {
                                if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Found '" + skill.name + "'"); }
                                if (skill.name == roll.name)
                                {
                                    if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Rolling '" + skill.name + "'"); }
                                    result = ExecuteRoll(skill.roll);
                                    if (showDiagnostics) { Debug.Log("D&D 5e Macros Plug-In: Queuing Rolling '" + StatMessaging.GetCreatureName(opposer) + ": " + roll.name + ": " + result.total + "'"); }
                                    MessageBoardPost(StatMessaging.GetCreatureName(opposer) + ": " + roll.name + ": " + result.total + ": " + ((result.total >= DC.total) ? "Opposed" : "Unopposed"), Audience.audience_secret, asset.Creature.CreatureId);
                                    break;
                                }
                            }
                        }
                        break;
                    default:
                        if (roll.roll != "")
                        {
                            result = ExecuteRoll(roll.roll);
                            content = roll.name + " " + result.total;
                            messageQueue.Add(new Tuple<NGuid,string>(new NGuid(LocalClient.SelectedCreatureId.ToString()), roll.name + " " + result.total));
                            content = content + "\r\n(" + result.expanded + ")";
                            if (result.minRoll) { content = content + "\r\n[Min Roll]"; }
                            if (result.minRoll) { content = content + "\r\n[Max Roll]"; }
                        }
                        else
                        {
                            content = roll.name;
                            messageQueue.Add(new Tuple<NGuid, string>(new NGuid(LocalClient.SelectedCreatureId.ToString()), content));
                        }
                        messageQueue.Add(new Tuple<NGuid, string>(NGuid.Empty, content));
                        break;
                }
                roll = roll.link;
            }
        }

        private RollResult ExecuteRoll(string rollSpecs, bool criticalHit = false)
        {
            string expanded = "";
            string component1 = "";
            string component2 = "";
            string operation = "";
            bool maxRoll = true;
            bool minRoll = true;
            while (rollSpecs != "")
            {
                component2 = "";
                while ("0123456789".Contains(rollSpecs.Substring(0, 1)))
                {
                    component2 = component2 + rollSpecs.Substring(0, 1);
                    rollSpecs = rollSpecs.Substring(1);
                    if (rollSpecs == "") { break; }
                }
                switch (operation)
                {
                    case "D":
                        expanded = expanded + component1 + "D" + component2 + "[";
                        int total = 0;
                        for (int d = 0; d < int.Parse(component1); d++)
                        {
                            int ran = random.Next(1, int.Parse(component2)+1);
                            if (ran != 1) { minRoll = false; }
                            if (ran != int.Parse(component2)) { maxRoll = false; }
                            total = total + ((criticalHit) ? ran*2 : ran);
                            expanded = expanded + ran + ",";
                        }
                        expanded = expanded.Substring(0, expanded.Length - 1) + "]"+((criticalHit)?"x2":"")+"=";
                        component2 = total.ToString();
                        break;
                    case "+":
                        expanded = expanded + component1 + "+" + component2 + "=";
                        component2 = (int.Parse(component1) + int.Parse(component2)).ToString();
                        break;
                    case "-":
                        expanded = expanded + component1 + "-" + component2 + "=";
                        component2 = (int.Parse(component1) - int.Parse(component2)).ToString();
                        break;
                    case "*":
                        expanded = expanded + component1 + "x" + component2 + "=";
                        component2 = (int.Parse(component1) * int.Parse(component2)).ToString();
                        break;
                    case "/":
                        expanded = expanded + component1 + "/" + component2 + "=";
                        component2 = (int.Parse(component1) / int.Parse(component2)).ToString();
                        break;
                    default:
                        break;
                }
                if (rollSpecs == "") { expanded = expanded.Substring(0, expanded.Length - 1); break; }
                operation = rollSpecs.Substring(0, 1);
                rollSpecs = rollSpecs.Substring(1);
                component1 = component2;
            }
            return new RollResult() { total = int.Parse(component2), expanded = expanded, maxRoll = maxRoll, minRoll = minRoll };
        }

        public string AddEnvelope(string message, int envelopeCount)
        {
            string result = "";
            for(int i=0; i<envelopeCount; i++)
            {
                result = result + "  ";
            }
            return (envelopeCount > 0) ? result + "\r\n" + message + "\r\n" + result : message;
        }

        public class RollResult
        {
            public int total { get; set; } = 0;
            public string expanded { get; set; } = "";
            public bool maxRoll = false;
            public bool minRoll = false;
        }
    }
}
