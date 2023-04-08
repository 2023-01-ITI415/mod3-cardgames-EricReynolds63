using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Original { 

	// This enum defines the variable type eCardState with four named values.
	public enum eCardState { drawpile, mine, target, discard }
	public enum eCardType { normal, silver }
 
	public class CardProspector : Card { // Make CardProspector extend Card
		[Header("Dynamic: CardProspector")]
		public eCardState           state = eCardState.drawpile;
		public eCardType            type = eCardType.normal;
		// The hiddenBy list stores which other cards will keep this one face down
		public List<CardProspector> hiddenBy = new List<CardProspector>();
		// The layoutID matches this card to the tableau JSON if itÅfs a tableau card
		public int                  layoutID;
		// The JsonLayoutSlot class stores information pulled in from JSON_Layout
		public JsonLayoutSlot       layoutSlot;

		/// <summary>
		/// Informs the Prospector class that this card has been clicked.
		/// </summary>
		override public void OnMouseUpAsButton() {
			// Uncomment the next line to call the base class version of this method
			// base.OnMouseUpAsButton();
			// Call the CardClicked method on the Prospector Singleton
			Prospector.CARD_CLICKED(this);
		}

		public void ConvertToSilver(CardProspector cp) {
			cp.type = eCardType.silver;
			cp.GetComponent<SpriteRenderer>().sprite = CardSpritesSO.SFRONT;
			cp.back.GetComponent<SpriteRenderer>().sprite = CardSpritesSO.SBACK;
			Debug.Log("Converted to Silver!");
		}
	}

}