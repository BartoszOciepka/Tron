using System.Collections;
using UnityEngine;
public class TowerAgent : MonoBehaviour
{
	public GameObject[] players = new GameObject[2];
	public bool[] helping = new bool[2];
	System.Random random = new System.Random();
	void Start()
	{
		chooseWhoToHelp();
		StartCoroutine(helpPlayers());
	}

	// Update is called once per frame
	void Update()
	{

	}

	public IEnumerator helpPlayers()
	{
		while (true)
		{
			for (int i = 0; i < players.Length; i++)
			{
				int enemyIndex;
				if (i == 0) enemyIndex = 1;
				else enemyIndex = 0;


				if (helping[i] == true) players[i].GetComponent<Tron.TronAgent>().receiveHelp(players[enemyIndex].GetComponent<Rigidbody2D>().transform.position.x,
					 players[enemyIndex].GetComponent<Rigidbody2D>().transform.position.y);
				else
					players[i].GetComponent<Tron.TronAgent>().receiveHelp(random.Next(-60, 60), random.Next(-60, 60));
			}

			yield return new WaitForSeconds(random.Next(10));
		}
	}

	public void chooseWhoToHelp()
	{
		for (int i = 0; i < players.Length; i++)
		{
			if (random.Next(50) < 50)
			{
				helping[i] = true;
			}
			else
				helping[i] = false;
		}
	}
}
