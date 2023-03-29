using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;   // We�fll need this line later in the chapter

[RequireComponent(typeof(Deck))]
[RequireComponent(typeof(JsonParseLayout))]
public class Prospector : MonoBehaviour {
    private static Prospector S; // A private Singleton for Prospector

    [Header("Inscribed")]
    public float                roundDelay = 2f;  // 2 sec delay between rounds
 
    [Header("Dynamic")]
    public List<CardProspector> drawPile;
    public List<CardProspector> discardPile;
    public List<CardProspector> mine; 
    public CardProspector       target;
 
    private Transform  layoutAnchor; 
    private Deck       deck;
    private JsonLayout jsonLayout;

	public bool	   firstCard;
	public CardProspector A = null;
	public CardProspector B = null;

    // A Dictionary to pair mine layout IDs and actual Cards
    private Dictionary<int, CardProspector> mineIdToCardDict;
 
    void Start () {
        // Set the private Singleton. We�fll use this later.
        if (S != null) Debug.LogError("Attempted to set S more than once!");
         S = this;
 
        jsonLayout = GetComponent<JsonParseLayout>().layout;
 
        deck = GetComponent<Deck>();
        // These two lines replace the Start() call we commented out in Deck
        deck.InitDeck();   
        Deck.Shuffle(ref deck.cards);
 
        drawPile = ConvertCardsToCardProspectors(deck.cards);

        LayoutMine();

		// DON'T Set up the initial target card
        //MoveToTarget( Draw () );
		firstCard = true;

        // Set up the draw pile
        UpdateDrawPile();
    }
	 
    /// <summary>
    /// Converts each Card in a List(Card) into a List(CardProspector) so that it
    ///  can be used in the Prospector game.
    /// </summary>
    /// <param name="listCard">A List(Card) to be converted</param>
    /// <returns>A List(CardProspector) of the converted cards</returns>
    List<CardProspector> ConvertCardsToCardProspectors(List<Card> listCard) {
        List<CardProspector> listCP = new List<CardProspector>();
        CardProspector cp;
        foreach( Card card in listCard) {
            cp = card as CardProspector;
            listCP.Add( cp );
        }
        return( listCP );
    }

    /// <summary>
    /// Pulls a single card from the beginning of the drawPile and returns it
    /// Note: There is no protection against trying to draw from an empty pile!
    /// </summary>
    /// <returns>The top card of drawPile</returns>
    CardProspector Draw() {
        CardProspector cp = drawPile[0]; // Pull the 0th CardProspector
        drawPile.RemoveAt(0);            // Then remove it from drawPile
        return(cp);                      // And return it
    }
 
    /// <summary>
    /// Positions the initial tableau of cards, a.k.a. the "mine"
    /// </summary>
    void LayoutMine() {
        // Create an empty GameObject to serve as an anchor for the tableau
        if (layoutAnchor == null) {
            // Create an empty GameObject named _LayoutAnchor in the Hierarchy
            GameObject tGO = new GameObject("_LayoutAnchor"); 
            layoutAnchor = tGO.transform;             // Grab its Transform
        }

        CardProspector cp;

        // Generate the Dictionary to match mine layout ID to CardProspector
        mineIdToCardDict = new Dictionary<int, CardProspector>();
 
        // Iterate through the JsonLayoutSlots pulled from the JSON_Layout
        foreach (JsonLayoutSlot slot in jsonLayout.slots) {
            cp = Draw(); // Pull a card from the top (beginning) of the draw Pile
            cp.faceUp = slot.faceUp;    // Set its faceUp to the value in SlotDef
            // Make the CardProspector a child of layoutAnchor
            cp.transform.SetParent(layoutAnchor);                          
 
            // Convert the last char of the layer string to an int (e.g. "Row 0")
            int z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());
 
            // Set the localPosition of the card based on the slot information
            cp.SetLocalPos( new Vector3(
                jsonLayout.multiplier.x * slot.x,
                jsonLayout.multiplier.y * slot.y,
                -z ) );

            cp.layoutID = slot.id;
            cp.layoutSlot = slot;
            // CardProspectors in the mine have the state CardState.mine
            cp.state = eCardState.mine;

            // Set the sorting layer of all SpriteRenderers on the Card
            cp.SetSpriteSortingLayer(slot.layer);

            mine.Add(cp); // Add this CardProspector to the List<> mine

            // Add this CardProspector to the mineIDtoCardDict Dictionary
            mineIdToCardDict.Add(slot.id, cp);
        }
    }

    /// <summary>
    /// Moves the current target card to the discardPile
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToDiscard(CardProspector cp) {
        // Set the state of the card to discard
        cp.state = eCardState.discard;
        discardPile.Add(cp);  // Add it to the discardPile List<>
        cp.transform.SetParent(layoutAnchor); // Update its transform parent

        // Position it on the discardPile
        cp.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * jsonLayout.discardPile.x,
            jsonLayout.multiplier.y * jsonLayout.discardPile.y,
            0));

        cp.faceUp = true;
 
        // Place it on top of the pile for depth sorting
        cp.SetSpriteSortingLayer(jsonLayout.discardPile.layer);
        cp.SetSortingOrder(-200 + (discardPile.Count * 3) );
    }
 
    /// <summary>
    /// Make cp the new target card
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToTarget(CardProspector cp) {
        // If there is currently a target card, move it to discardPile
        if (target != null) MoveToDiscard(target);

        // Use MoveToDiscard to move the target card to the correct location
        MoveToDiscard(cp);
 
        // Then set a few additional things to make cp the new target
        target = cp; 
        cp.state = eCardState.target;
 
        // Set the depth sorting so that cp is on top of the discardPile
        cp.SetSpriteSortingLayer( "Target" );
        cp.SetSortingOrder(0);
    }
 
    /// <summary>
    /// Arranges all the cards of the drawPile to show how many are left
    /// </summary>
    void UpdateDrawPile() {
        CardProspector cp;
        // Go through all the cards of the drawPile
        for (int i = 0; i < drawPile.Count; i++) {
             cp = drawPile[i];
            cp.transform.SetParent(layoutAnchor);

            // Position it correctly with the layout.drawPile.stagger
            Vector3 cpPos = new Vector3();
            cpPos.x = jsonLayout.multiplier.x * jsonLayout.drawPile.x;
            // Add the staggering for the drawPile
            cpPos.x += jsonLayout.drawPile.xStagger * i;
            cpPos.y = jsonLayout.multiplier.y * jsonLayout.drawPile.y;
            cpPos.z = 0.1f * i;
            cp.SetLocalPos(cpPos);
 
            // DrawPile Cards are all face-down
			cp.faceUp = false;
			if (i == 0) cp.faceUp = true;
			
            cp.state = eCardState.drawpile;
            // Set depth sorting
            cp.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            cp.SetSortingOrder(-10 * i);
        }
    }

    /// <summary>
    /// This turns cards in the Mine face-up and face-down
    /// </summary>
    public void SetMineFaceUps() {
        CardProspector coverCP;
        foreach (CardProspector cp in mine) {
            bool faceUp = true; // Assume the card will be face-up
 
            // Iterate through the covering cards by mine layout ID
            foreach (int coverID in cp.layoutSlot.hiddenBy) {
                coverCP = mineIdToCardDict[coverID];
                // If the covering card is null or still in the mine...
                if (coverCP == null || coverCP.state == eCardState.mine) {
                    faceUp = false; // then this card is face-down
                }
            }
            cp.faceUp = faceUp; // Set the value on the card
        }
    }

    /// <summary>
    /// Test whether the game is over
    /// </summary>
    void CheckForGameOver() {
        // If the mine is empty, the game is over
        if ( mine.Count == 0 ) {
            GameOver( true );  // Call GameOver() with a win
            return;
        }
 
        // If there are still cards in the mine & draw pile, the game�fs not over
        if ( drawPile.Count > 0 ) return;

        // Check for remaining valid plays
        foreach ( CardProspector cp in mine ) {
            // If there is a valid play, the game�fs not over
            if ( target.AdjacentTo( cp ) ) return;
        }
 
        // Since there are no valid plays, the game is over
        GameOver( false );  // Call GameOver with a loss
    }

    /// <summary>
    /// Called when the game is over. Simple for now, but expandable
    /// </summary>
    /// <param name="won">true if the player won</param>
    void GameOver( bool won ) {
        if ( won ) {
            ScoreManager.TALLY( eScoreEvent.gameWin );
        } else {
            ScoreManager.TALLY( eScoreEvent.gameLoss );
        }
 
        // Reset the CardSpritesSO singleton to null
        CardSpritesSO.RESET();
        Invoke ("ReloadLevel", roundDelay);
        // SceneManager.LoadScene("__Prospector_Scene_0");

		UITextManager.GAME_OVER_UI(won);
    }

    void ReloadLevel() {
        // Reload the scene, resetting the game
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    /// <summary>
    /// Handler for any time a card in the game is clicked
    /// </summary>
    /// <param name="cp">The CardProspector that was clicked</param>
    static public void CARD_CLICKED(CardProspector cp) {
        // The reaction is determined by the state of the clicked card
        switch (cp.state) {
        case eCardState.target:
			// Do nothing
        case eCardState.drawpile:
			if (S.firstCard == true) {
				S.A = S.drawPile[0];  // Take top draw pile card to compare with
				S.firstCard = false;
			} else {
				S.B = S.drawPile[0];
				if (S.A.AdjacentTo(S.B) || S.B.rank == 13) {        // If it�fs a valid card
					if (S.A != null) {		// In the case of a King solo match, there is no B
						S.mine.Remove(S.A);   // Remove it from the tableau List
						S.MoveToTarget(S.A);  // Make it the target card
						S.A = null;
					}
					S.MoveToTarget(S.B);  // Make it the target card
					if (S.B == S.drawPile[0]) {
						S.drawPile.RemoveAt(0);
						S.UpdateDrawPile();
					}
					S.B = null;
					S.firstCard = true;
					ScoreManager.TALLY( eScoreEvent.mine );
				} else {
					S.A = null;
					S.B = null;
					S.firstCard = true;
				}
			}
            
            break;
        case eCardState.mine:
            // Clicking a card in the mine will check if it�fs a valid play
            bool validMatch = true;  // Initially assume that it�fs valid 
 
            // If the card is face-down, it�fs not valid
            if (!cp.faceUp) validMatch = false;

			// CHECK IF FIRST OR SECOND CARD OF MATCH
			if (S.firstCard) {		//Exception covered below for King, which is a 1 card match
				S.A = cp;	//Save as Card A
				if (cp.rank == 13) {
					validMatch = true;
					S.firstCard = true;
				} else {
					validMatch = false;
					S.firstCard = false;
				}
				//if (cp.rank != 13) { validMatch = true; }
			} else {
				S.B = cp;	//Save as Card B
				if (!cp.AdjacentTo(S.A)) {	//If not a match
					validMatch = false; 
					if (S.B != null) {		//If not a match, after checking both cards
						S.A = null;
						S.B = null;
						S.firstCard = true;
						break;
					}
				}
				if (validMatch) { S.firstCard = true; }
			}
 
            if (validMatch) {        // If it�fs a valid card
				if (S.A == S.drawPile[0]) {
					S.drawPile.RemoveAt(0);
					S.UpdateDrawPile();
				}
                S.mine.Remove(S.A);   // Remove it from the tableau List
				S.MoveToTarget(S.A);  // Make it the target card
				S.A = null;
				if (S.B != null) {		// In the case of a King solo match, there is no B
					S.mine.Remove(S.B);   // Remove it from the tableau List
					S.MoveToTarget(S.B);  // Make it the target card
					S.B = null;
				}
				
                ScoreManager.TALLY( eScoreEvent.mine );
            }
            break;
        }
        S.CheckForGameOver();
    }

}