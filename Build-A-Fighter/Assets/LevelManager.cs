﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {

	WaitForSeconds oneSec; //we will be using this a lot so we don't want to create new ones all the time
	public Transform[] spawnPosition; // the position characters will spawn on

	CharacterManager charM;
	LevelUI levelUI; //we store ui elements her for ease of access

	public int maxTurns = 2;
	int currentTurn = 1; //the current turn we are in

	//variables for the countdown
	public bool countdown;
	public int maxTurnTimer = 10;
	int currentTimer;
	float internalTimer;

	// Use this for initialization
	void Start () {
		charM = CharacterManager.GetInstance();
		levelUI = LevelUI.GetInstance();

		oneSec = new WaitForSeconds(1);

		levelUI.AnnouncerTextLine1.gameObject.SetActive(false);
		levelUI.AnnouncerTextLine2.gameObject.SetActive(false);

		StartCoroutine("StartGame");
	}

	private void FixedUpdate()
	{
		//A fast way to handle player orientation in the scene
		//just compare the x of the first player, if it's lower than the enemy is on the right

		if(charM.players[0].playerStates.transform.position.x < charM.players[1].playerStates.transform.position.x)
		{
			charM.players[0].playerStates.lookRight = true;
			charM.players[1].playerStates.lookRight = false;
		}
		else
		{
			charM.players[0].playerStates.lookRight = false;
			charM.players[1].playerStates.lookRight = true;
		}
	}

	// Update is called once per frame
	void Update ()
	{
		if(countdown)//if we enable countdown
		{
			HandleTurnTimer();//control the timer here
		}
	}

	void HandleTurnTimer()
	{
		levelUI.LevelTimer.text = currentTimer.ToString();

		internalTimer += Time.deltaTime; //every one second

		if(internalTimer > 1)
		{
			currentTimer--; //substract from the current timer one
			internalTimer = 0;
		}

		if(currentTimer <= 0) //if the countdown is over
		{
			EndTurnFunction(true);//end the turn
			countdown = false;
		}
	}

	IEnumerator StartGame()
	{
		//when we first start the game

		//we need to create the players first
		yield return CreatePlayers();

		//then initialize the turn
		yield return InitTurn();
	}

	IEnumerator CreatePlayers()
	{
		//go to all the players we have in our list
		for(int i = 0; i < charM.players.Count; i++)
		{
			//and instantiate their prefabs
			GameObject go = Instantiate(charM.players[i].playerPrefab, spawnPosition[i].position, Quaternion.identity) as GameObject;

			//and assign the needed references
			charM.players[i].playerStates = go.GetComponent<StateManager>();

			charM.players[i].playerStates.healthSlider = levelUI.healthSliders[i];

		}

		yield return null;
	}

	IEnumerator InitTurn()
	{
		//to init the turn

		//disable the announcer texts first
		levelUI.AnnouncerTextLine1.gameObject.SetActive(false);
		levelUI.AnnouncerTextLine2.gameObject.SetActive(false);

		//reset the timer
		currentTimer = maxTurnTimer;
		countdown = false;

		//start initializing the players
		yield return InitPlayers();

		//and then start the coroutine to enable the controls of each player
		yield return EnableControl();
	}

	IEnumerator InitPlayers()
	{
		//right now, the only thing we have to do is reset their health
		for(int i = 0; i < charM.players.Count; i++)
		{
			charM.players[i].playerStates.health = 100;
			charM.players[i].playerStates.handleAnim.anim.Play("Locomotion");
			charM.players[i].playerStates.transform.position = spawnPosition[i].position;
		}

		yield return null;
	}

	IEnumerator EnableControl()
	{
		//start with the announcer text

		levelUI.AnnouncerTextLine1.gameObject.SetActive(true);
		levelUI.AnnouncerTextLine1.text = "Turn " + currentTurn;
		levelUI.AnnouncerTextLine1.color = Color.white;
		yield return oneSec;
		yield return oneSec;

		//change the UI text and color every second that passes
		levelUI.AnnouncerTextLine1.text = "3";
		levelUI.AnnouncerTextLine1.color = Color.green;
		yield return oneSec;
		levelUI.AnnouncerTextLine1.text = "2";
		levelUI.AnnouncerTextLine1.color = Color.yellow;
		yield return oneSec;
		levelUI.AnnouncerTextLine1.text = "1";
		levelUI.AnnouncerTextLine1.color = Color.red;
		yield return oneSec;
		levelUI.AnnouncerTextLine1.color = Color.red;
		levelUI.AnnouncerTextLine1.text = "FIGHT!";

		//and for every player enable what they need to have open to be controlled
		for (int i = 0; i < charM.players.Count; i++)
		{
			//for user players, enable the input handler for example
			if(charM.players[i].playerType == PlayerBase.PlayerType.user)
			{
				InputHandler ih = charM.players[i].playerStates.gameObject.GetComponent<InputHandler>();
				ih.playerInput = charM.players[i].inputId;
				ih.enabled = true;
			}
		}

		//after a second, disable the announcer text
		yield return oneSec;
		levelUI.AnnouncerTextLine1.gameObject.SetActive(false);
		countdown = true;
	}

	void DisableControl()
	{
		//to disable the controls, you need to disable the component that makes a character
		for(int i = 0; i < charM.players.Count; i++)
		{
			//but first, reset the variables in their state manager
			charM.players[i].playerStates.ResetStateInputs();

			//for user players, that's the input handler
			if(charM.players[i].playerType == PlayerBase.PlayerType.user)
			{
				charM.players[i].playerStates.GetComponent<InputHandler>().enabled = false;
			}
		}
	}

	public void EndTurnFunction(bool timeOut = false)
	{
		/*We call this function everytime we want to end the turn
		 * but we need to know if we do so by a timeout or not
		 */
		countdown = false;
		//reset the timer text
		levelUI.LevelTimer.text = maxTurnTimer.ToString();

		//if it's a timeout
		if(timeOut)
		{
			//add this text first
			levelUI.AnnouncerTextLine1.gameObject.SetActive(true);
			levelUI.AnnouncerTextLine1.text = "Time Out!";
			levelUI.AnnouncerTextLine1.color = Color.cyan;
		}
		else
		{
			levelUI.AnnouncerTextLine1.gameObject.SetActive(true);
			levelUI.AnnouncerTextLine1.text = "K.O.";
			levelUI.AnnouncerTextLine1.color = Color.red;
		}

		//disable the controlls
		DisableControl();

		//and start the coroutine for end turn
		StartCoroutine("EndTurn");
	}

	IEnumerator EndTurn()
	{
		//wait 3 seconds for the previous text to clearly show
		yield return oneSec;
		yield return oneSec;
		yield return oneSec;

		//find who was the player that won
		PlayerBase vPlayer = FindWinningPlayer();

		if(vPlayer == null) //if our function returned a null
		{
			//that means it's a draw
			levelUI.AnnouncerTextLine1.text = "Draw";
			levelUI.AnnouncerTextLine1.color = Color.blue;
		}
		else
		{
			//else that player is the winner
			levelUI.AnnouncerTextLine1.text = vPlayer.playerId + " Wins!";
			levelUI.AnnouncerTextLine1.color = Color.red;
		}

		//wait 3 more seconds
		yield return oneSec;
		yield return oneSec;
		yield return oneSec;

		//check to see if the victorious player has taken any damage
		if(vPlayer != null)
		{
			//if not, then it's a flawless victory
			if(vPlayer.playerStates.health == 100)
			{
				levelUI.AnnouncerTextLine2.gameObject.SetActive(true);
				levelUI.AnnouncerTextLine2.text = "Flawless Victory!";
			}
		}

		//wait 3 seconds
		yield return oneSec;
		yield return oneSec;
		yield return oneSec;


		currentTurn++;
		bool matchOver = isMatchOver();
		if(!matchOver)
		{
			StartCoroutine("InitTurn");
		}
		else
		{
			for(int i = 0; i < charM.players.Count; i++)
			{
				charM.players[i].score = 0;
				charM.players[i].hasCharacter = false;
			}

			SceneManager.LoadSceneAsync("select");
		}
	}

	bool isMatchOver()
	{
		bool retVal = false;

		for(int i = 0; i < charM.players.Count; i++)
		{
			if(charM.players[i].score >= maxTurns)
			{
				retVal = true;
				break;
			}
		}

		return retVal;
	}

	PlayerBase FindWinningPlayer()
	{
		//to find who won the turn
		PlayerBase retVal = null;

		StateManager targetPlayer = null;

		//check first to see if both players have equal health
		if(charM.players[0].playerStates.health != charM.players[1].playerStates.health)
		{
			//if not, then check who has the lower health, the other one is the winner
			if(charM.players[0].playerStates.health < charM.players[1].playerStates.health)
			{
				charM.players[1].score++;
				targetPlayer = charM.players[1].playerStates;
				levelUI.AddWinIndicator(1);
			}
			else
			{
				charM.players[0].score++;
				targetPlayer = charM.players[0].playerStates;
				levelUI.AddWinIndicator(0);
			}

			retVal = charM.returnPlayerFromStates(targetPlayer);
		}

		return retVal;
	}


	public static LevelManager instance;
	public static LevelManager GetInstance()
	{
		return instance;
	}

}
