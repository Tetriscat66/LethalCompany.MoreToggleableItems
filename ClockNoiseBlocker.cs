using UnityEngine;

namespace MoreToggleableItems {
	public class ClockNoiseBlocker : MonoBehaviour {
		public bool blockClockTicking;
		public float timeUntilNextSecond;

		public void Initialize(GrabbableObject obj, int offChance) {
			System.Random itemRandomChance = new System.Random(StartOfRound.Instance.randomMapSeed + StartOfRound.Instance.currentLevelID + obj.itemProperties.itemId + (int)(transform.position.x + transform.position.z) + obj.scrapValue);
			blockClockTicking = itemRandomChance.Next(0, 100) < offChance;
			timeUntilNextSecond = 1f;
		}
	}
}
