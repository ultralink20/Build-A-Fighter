using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SelectScreenManager : MonoBehaviour
{

	public int numberOfPlayers = 1;
	public List<PlayerInterfaces> plInterfaces;
	public PotraitInfo[] potraitPrefabs; //All our entries as potraits
	public int maxX; //how many potraits we have on the X and Y
	public int maxY;
	PotraitInfo[,] charGrid; //the grid we are making to select entries

	public GameObject potraitCanvas; //the canvas that holds all the potraits

	bool loadLevel; //if we are loading the level
	public bool bothPlayersSelected;

	CharacterManager charManager;

	#region Singleton
	public static SelectScreenManager instance;
	public static SelectScreenManager GetInstance()
	{
		return instance;
	}

	void Awake()
	{
		instance = this;
	}
	#endregion

	// Use this for initialization
	void Start () {
		//we start by getting the reference to the character manager
		charManager = CharacterManager.GetInstance();
		numberOfPlayers = charManager.numberOfUsers;

		//and we create the grid
		charGrid = new PotraitInfo[maxX, maxY];

		int x = 0;
		int y = 0;

		potraitPrefabs = potraitCanvas.GetComponentsInChildren<PotraitInfo>();

		//we need to go into all our potraits
		for(int i = 0; i <  potraitPrefabs.Length; i++)
		{
			//and assign a grid position
			potraitPrefabs[i].posX += x;
			potraitPrefabs[i].posY += y;

			charGrid[x, y] = potraitPrefabs[i];

			if (x < maxX - 1)
			{
				x++;
			}
			else
			{
				x = 0;
				y++;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(!loadLevel)
		{
			for (int i = 0; i <plInterfaces.Count; i++)
			{
				if(i < numberOfPlayers)
				{
					if(Input.GetButtonUp("Fire2" + charManager.players[i].inputId))
					{
						plInterfaces[i].playerBase.hasCharacter = false;
					}
					if(!charManager.players[i].hasCharacter)
					{
						plInterfaces[i].playerBase = charManager.players[i];

						HandleSelectorPosition(plInterfaces[i]);
						HandleSelectScreenInput(plInterfaces[i], charManager.players[i].inputId);
						HandleCharacterPreview(plInterfaces[i]);
					}
				}
				else
				{
					charManager.players[i].hasCharacter = true;
				}
			}
		}
		if(bothPlayersSelected)
		{
			Debug.Log("loading");
			StartCoroutine("LoadLevel"); //and start the coroutine to lpad the level
			loadLevel = true;
		}
		else
		{
			if(charManager.players[0].hasCharacter && charManager.players[1].hasCharacter)
			{
				bothPlayersSelected = true;
			}
		}
	}

	void HandleSelectScreenInput(PlayerInterfaces pl, string PlayerId)
	{
		#region Grid Navigation
		/* to navigate in the grid
		 * we simply change the active x and y to select what entry is active
		 * we also smooth out the inputso if the user keeps pressing the button
		 * it won't switch more than once over half a second
		 */
		float vertical = Input.GetAxis("Vertical" + PlayerId);

		if (vertical != 0)
		{
			if(!pl.hitInputOnce)
			{
				if(vertical > 0)
				{
					pl.activeY = (pl.activeY > 0) ? pl.activeY - 1 : maxY - 1;
				}
				else
				{
					pl.activeY = (pl.activeY < maxY - 1) ? pl.activeY + 1 : 0;
				}

				pl.hitInputOnce = true;
			}
		}

		float horizontal = Input.GetAxis("Horizontal" + PlayerId);

		if (horizontal != 0)
		{
			if(!pl.hitInputOnce)
			{
				if(horizontal > 0)
				{
					pl.activeX = (pl.activeX > 0) ? pl.activeX - 1 : maxX - 1;
				}
				else
				{
					pl.activeX = (pl.activeX < maxX - 1) ? pl.activeX + 1 : 0;
				}

				pl.timerToReset = 0;
				pl.hitInputOnce = true;
			}
		}

		if(vertical == 0 && horizontal == 0)
		{
			pl.hitInputOnce = false;
		}

		if(pl.hitInputOnce)
		{
			pl.timerToReset += Time.deltaTime;

			if(pl.timerToReset > 0.8f)
			{
				pl.hitInputOnce = false;
				pl.timerToReset = 0;
			}
		}

		#endregion

		//if the user presses space, he has selected a character
		if(Input.GetButtonUp("Fire1" + PlayerId))
		{
			//make a reaction on the character
			pl.createdCharacter.GetComponentInChildren<Animator>().Play("Kick");

			//pass the character to the character manager so that we know what prefab to create in the match
			pl.playerBase.playerPrefab = charManager.returnCharacterWithID(pl.activePotrait.characterId).prefab;

			pl.playerBase.hasCharacter = true;
		}
	}

	IEnumerator LoadLevel()
	{
		//if any of the player is an AI, then assign a random character to the prefab
		for (int i = 0; i < charManager.players.Count; i++)
		{
			if(charManager.players[i].playerType == PlayerBase.PlayerType.ai)
			{
				if(charManager.players[i].playerPrefab == null)
				{
					int ranValue = Random.Range(0, potraitPrefabs.Length);

					charManager.players[i].playerPrefab = charManager.returnCharacterWithID(potraitPrefabs[ranValue].characterId).prefab;

					Debug.Log(potraitPrefabs[ranValue].characterId);
				}
			}
		}

		yield return new WaitForSeconds(2); //after 2 seconds load the level
		SceneManager.LoadSceneAsync("level", LoadSceneMode.Single);
	}

	void HandleSelectorPosition(PlayerInterfaces pl)
	{
		pl.selector.SetActive(true); //enable the selector

		pl.activePotrait = charGrid[pl.activeX, pl.activeY]; //find the active potrait

		//and place the selector over it's position
		Vector2 selectorPosition = pl.activePotrait.transform.localPosition;
		selectorPosition = selectorPosition + new Vector2(potraitCanvas.transform.localPosition.x, potraitCanvas.transform.localPosition.y);

		pl.selector.transform.localPosition = selectorPosition;
	}

	void HandleCharacterPreview(PlayerInterfaces pl)
	{
		//if the previous potrait we had, is not the same as the active one we have
		//that means we changed the characters
		if(pl.previewPotrait != pl.activePotrait)
		{
			if(pl.createdCharacter != null) //delete the one we have now if we do have one
			{
				Destroy(pl.createdCharacter);
			}

			//and create another one
			GameObject go = Instantiate(CharacterManager.GetInstance().returnCharacterWithID(pl.activePotrait.characterId).prefab, pl.charVisPos.position, Quaternion.identity) as GameObject;

			pl.createdCharacter = go;

			pl.previewPotrait = pl.activePotrait;

			if(!string.Equals(pl.playerBase.playerId, charManager.players[0].playerId))
			{
				pl.createdCharacter.GetComponent<StateManager>().lookRight = false;
			}
		}
	}

}

[System.Serializable]
public class PlayerInterfaces
{
	public PotraitInfo activePotrait; //the current active potrait for player 1
	public PotraitInfo previewPotrait;
	public GameObject selector; //the select indicator for player 1
	public Transform charVisPos; //the visualization position for player 1
	public GameObject createdCharacter; //the created character for player 1

	public int activeX; //the active X and Y entries for player 1
	public int activeY;

	//variables for smoothing out input
	public bool hitInputOnce;
	public float timerToReset;

	public PlayerBase playerBase;
}