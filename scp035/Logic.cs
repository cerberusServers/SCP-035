﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

namespace scp035
{
	partial class EventHandlers
	{
		private void RemovePossessedItems()
		{
			for (int i = 0; i < scpPickups.Count; i++)
			{
				Pickup p = scpPickups.ElementAt(i).Key;
				if (p != null) p.Delete();
			}
			scpPickups.Clear();
		}

		private Pickup GetRandomItem()
		{
			List<Pickup> pickups = GameObject.FindObjectsOfType<Pickup>().Where(x => !scpPickups.ContainsKey(x)).ToList();
			return pickups[rand.Next(pickups.Count)];
		}

		private void RefreshItems()
		{
			// Check if player is in Overwatch mode, don't let them in the list if they are
			if (Plugin.GetHubs().Where(x => x.GetTeam() == Team.RIP).ToList().Count > 0)
			{
				RemovePossessedItems();
				for (int i = 0; i < Configs.infectedItemCount; i++)
				{
					Pickup p = GetRandomItem();
					Pickup a = PlayerManager.localPlayer
						.GetComponent<Inventory>().SetPickup((ItemType)Configs.possibleItems[rand.Next(Configs.possibleItems.Count)],
						-4.656647E+11f,
						new Vector3(p.transform.position.x, p.transform.position.y, p.transform.position.z),
						new Quaternion(p.transform.rotation.x, p.transform.rotation.y, p.transform.rotation.z, p.transform.rotation.w),
						0, 0, 0).GetComponent<Pickup>();
					scpPickups.Add(a, a.info.durability);
					a.info.durability = dur;
				}
			}
			Plugin.Info("finished");
		}

		private void KillScp035(bool setRank = true)
		{
			if (setRank)
			{
				scpPlayer.serverRoles.HiddenBadge = null;
				scpPlayer.serverRoles.RpcResetFixed();
				scpPlayer.serverRoles.RefreshPermissions(true);
				if (isHidden)
				{
					scpPlayer.serverRoles.HiddenBadge = scpPlayer.serverRoles.MyText;
					scpPlayer.serverRoles.NetworkGlobalBadge = null;
					scpPlayer.serverRoles.SetText(null);
					scpPlayer.serverRoles.SetColor(null);
					scpPlayer.serverRoles.GlobalSet = false;
					scpPlayer.serverRoles.RefreshHiddenTag();
				}
			}
			scpPlayer = null;
			isRotating = true;
			RefreshItems();
		}

		private void InfectPlayer(ReferenceHub player, Pickup pItem)
		{
			// Check if player is in Overwatch mode, don't let them in the list if they are
			List<ReferenceHub> pList = Plugin.GetHubs().Where(x => x.characterClassManager.CurClass == RoleType.Spectator).ToList();
			if (pList.Count > 0 && scpPlayer == null)
			{
				pItem.Delete();
				ReferenceHub p035 = pList[rand.Next(pList.Count)];
				Vector3 pos = player.GetPosition();
				p035.ChangeRole(player.characterClassManager.CurClass);
				Timing.RunCoroutine(DelayAction(0.2f, () => p035.SetPosition(pos)));
				foreach (Inventory.SyncItemInfo item in player.GetInventory()) p035.GiveItem(item.id);
				p035.SetHealth(Configs.health);
				p035.SetAmmo(AmmoType.DROPPED_5, player.GetAmmo(AmmoType.DROPPED_5));
				p035.SetAmmo(AmmoType.DROPPED_7, player.GetAmmo(AmmoType.DROPPED_7));
				p035.SetAmmo(AmmoType.DROPPED_9, player.GetAmmo(AmmoType.DROPPED_9));
				isHidden = p035.serverRoles.HiddenBadge != null;
				p035.serverRoles.HiddenBadge = null;
				p035.SetRank("SCP-035", "red");
				p035.Broadcast("<size=60>You are <color=\"red\"><b>SCP-035</b></color></size>\nYou have infected a body and have gained control over it, use it to help the other SCPs!", 10);
				scpPlayer = p035;
				isRotating = false;

				player.ChangeRole(RoleType.Spectator);
				player.Broadcast("You have picked up <color=\"red\">SCP-035.</color> He has infected your body and is now in control of you.", 10);

				RemovePossessedItems();
			}
		}

		private IEnumerator<float> RotatePickup()
		{
			while (isRoundStarted)
			{
				if (isRotating)
				{
					RefreshItems();
				}
				yield return Timing.WaitForSeconds(Configs.rotateInterval);
			}
		}

		private IEnumerator<float> CorrodeUpdate()
		{
			while (isRoundStarted && scpPlayer != null && Configs.corrodePlayers)
			{
				IEnumerable<ReferenceHub> pList = Plugin.GetHubs().Where(x => x.GetPlayerId() != scpPlayer.GetPlayerId());
				if (!Configs.scpFriendlyFire) pList = pList.Where(x => x.GetTeam() != Team.SCP);
				if (!Configs.tutorialFriendlyFire) pList = pList.Where(x => x.GetTeam() != Team.TUT);
				foreach (ReferenceHub player in pList)
				{
					if (player != null && Vector3.Distance(scpPlayer.GetPosition(), player.transform.position) <= Configs.corrodeDistance)
					{
						CorrodePlayer(player);
					}
				}
				yield return Timing.WaitForSeconds(Configs.corrodeInterval);
			}
		}

		public static IEnumerator<float> DelayAction(float delay, Action x)
		{
			yield return Timing.WaitForSeconds(delay);
			x();
		}

		private void CorrodePlayer(ReferenceHub player)
		{
			// redo this part using new damage system
			if (Configs.corrodeLifeSteal && scpPlayer != null)
			{
				int currHP = scpPlayer.GetHealth();
				scpPlayer.SetHealth(currHP + Configs.corrodeDamage > Configs.health ? Configs.health : currHP + Configs.corrodeDamage);
			}
		}

		private void GrantFF(ReferenceHub player)
		{
			player.weaponManager.NetworkfriendlyFire = false;
			ffPlayers.Remove(player.GetPlayerId());
		}
	}
}
