# D&D 5E Macros Plugin

This unofficial TaleSpire plugin for implementing some D&D 5E rule automation. Currently provides:

1. Automated attack macros which roll attacks, compare them to target AC, roll damage, apply immunities and resistances,
   and adjusts the target HP by the resulting amount. Supports automatic dice doubling for critical hits and critical hit
   immunity. The attack process is documented in speech bubbles and the actual dice rolls are recorded in the chat.
   Hit and miss animation included.
   
2. Skill rolls which support public (all players and GM), private (only player and GM) and secret (only GM) rolls. Each
   roll can provide static messages to others (e.g. public announcement of Stealth but only GM sees result).
   
3. Single target or everyone opposed skill checks. The user rolls a skills and if the skill has an opposed skill it will
   be rolled for the target or everyone who has the skill. The results are shown the to GM which include the user skill
   roll, the rolls for all the opposers and a opposed or unopposed result.   

Video Preview: https://youtu.be/NS3wHFoChdw

## Change Log

1.5.0: Added opposed skill checks.

1.4.1: Added missing skills icon.

1.4.0: Added skills support.

1.3.0: Corrected sample files (Jon and Goblin).

1.2.0: Corrected README formatting.

1.1.0: Initial release

## Install

Use R2ModMan or similar installer to install this plugin.

Create a Dnd5e file for each character (or foe type) that is to use this plugin. See the included Jon.Dnd5e file as an
example. While the format does support skills, they are currently not used.

## Usage

1. Select the mini that is attacking.
2. Right click the mini that is to be attacked.
3. Select the Attacks menu and then the desired attack from the sub-menu.
4. The attack will be processed and the results displayed in speech bubbles with additional details in the chat.

## File Format

```
{
	"NPC": false,
	"attacks":
	[
		{
			"name": "Shortsword",
			"type": "Melee",
			"roll": "1D20+5",
			"link":
			{
				"name": "Damage",
				"type": "Piercing",
				"roll": "1D6+4",
			}
		},
		{
			"name": "Shortsword & Sneak",
			"type": "Melee",
			"roll": "1D20+5",
			"link":
			{
				"name": "Damage",
				"type": "Slashing",
				"roll": "1D6+4",
				"link":
				{
					"name": "Sneak Attack",
					"type": "Slashing",
					"roll": "2D6"
				}
			}
		},
	],
	"skills":
	[
		{
			"name": "Stealth",
			"type": "Public",
			"roll": "",
			"link":
				{
					"name": "Stealth",
					"type": "Secret",
					"roll": "1D20+5"
				}
		},
		{
			"name": "Slight Of Hand",
			"type": "Public",
			"roll": "",
			"link":
				{
					"name": "Slight Of Hand",
					"type": "Private",
					"roll": "1D20+7"
				}
		},
		{
			"name": "Perception",
			"type": "Public",
			"roll": "1D20+3"
		},
		{
			"name": "Deception",
			"type": "Public",
			"link":
				{
					"name": "Deception",
					"type": "Secret",
					"roll": "1D20+3",
					"link":
					{
						"name": "Insight",
						"type": "Opposed"
					}
				}
		}
	],
	"immunity":
	[
		"Force"
	],
	"resistance":
	[
		"Piercing"
	]
}
```

"NPC" indicates if additional information (like AC and remaining HP) are displayed or not. Typically this is set to true
for NPCs (i.e. character sheets used by the GM for enemies) so that the PC don't know the AC and HP of the foes. For
PC this is typically set to false to provide the players additional information when their character is attacked.

"attacks" is an array of Roll objects which define possible attacks the user can make.

"name" is a Roll object that determines the name of the attack whch will be displayed in the radial menu.
"type" is a Roll obejct that determines the type of attack typically unarmed, melee, range and magic.
"roll" is a Roll object that determines the roll that is made when this attack is selected. Uses the #D#+# or #D#-# format.
       It should be noted that the number before D is not optional. For example, 1D20 cannot be abbreviated with D20.
"link" is a Roll object links to the Roll damage object. This follows the same rules as a Roll object except the type
       determines the damage type and the roll rolls the weapon damage. The link in a Roll damage object can be used
	   to add additional damage (of the same or different type). This is typically used for things like a sword of flame
	   (where the weapon damage type and the bonus damage type are different) or to add extra damage like sneak damage.

"skills" is a Roll object that determines the skill to be rolled and how the results are displayed. The name proeprty
         indicates the name of the skill and is used in the output results. Type is one of "public", "private" (or "owner")
		 or "secret" (or "GM"). Public rolls are displayed as speech bubbles and chat messages for everyone to see.
		 Private rolls appear on a message board that is displayed only for the owner of the mini and the GM. The contents
		 is displayed for a configurable amount of time on the message board but can be dismissed by clicking on the message
		 board. Secret rolls also show up on the message board but only for the GM. The roll property is used to determines
		 the dice and modifier used to make the skill check. If the roll is empty, the roll name will be displayed as a
		 comment. The link property can be used to link to additions rolls which are automatically processed. The link
		 property for skills is typically used to display a comment for the public but display the roll results for the
		 owner and/or GM.

"immunity" is a list of strings representing damage types from which the user takes no damage. When the damage type
           of an attack against the user matches an immunity (exactly) the damage is reduced to 0.
		   
"resistance" os a list of strings representing damage types from which the user takes 1/2 damage. When the damage type
           of an attack against the user matches a resistance (exactly) the damage is reduced to 1/2.
				
Note: Immunity and resistance is only applied to the portion of damage that matches the damage type. If an attack does
      multiple types of damage, the plugin will correctly apply immunity and resistance to only the mathcing damage type.
	  
## Skill Icons

If a PNG file with the same name as a skill is present, that PNG file will be used for the icon file. If the skill does
not have a corresponding PNG file then the default skills icon will be used.
	  
## Limitations

1. While the plugin does expose the characters dictionary (so other plugins can modify it) this plugin reads the contents
   of the Dnd5E files at start up and does not provide any interactive methods to change the settings. For example, a new
   resistance gained through a spell would not be reflected.    
2. The attack sequence does not provide an option for reactions to be used to modify the attack sequence. For example,
   if the user casts a Shield spell to temporarily increase AC or uses a effects of a Warding Flare.
3. Currently does not support damage reduction such as that given by the Heavy Armor Master feat.
4. Opposed skills are roller per character sheet thus all characters using the same character sheet are assumed to have
   the same result.

### Work-Around: Changing Specifications

Sometimes a character will frequently change its statistics which affect attacks. For example, a Barbarian gains resistance
to physical attacks when raging but not when he/she is not raging. Since the plugin does not provide an interactive way to
change a charcters specifications (inclduing resistance) while running it may seem that such a character design is not
supported. However, there are a couple work arounds to get such characters working with this plugin.

#### Damage Changes

Changes to damage such as a rogue attack with and without sneak or barbarian adding damage from rage or not can be solved
by making two (or more) attack entries and using the appropriate one. This fills up the radial menu quickly if you have
many combinations but allows such a character to be used with this plugin.

#### Immunity And Resistance Changes

To create a character that can change immunities and/or resistances, such as a barbarian, create two copies of the same
character with slightly different names. For example, "Garth" and "Garth (Rage)". Set the appropriated immunities and
resistances for version. In game, one can switch between the different modes by renaming the character. Since the plugin
looks up the character sheet associated with the mini name, it is possible to access multiple version of a character just
by renaming the character.
each. 
   
