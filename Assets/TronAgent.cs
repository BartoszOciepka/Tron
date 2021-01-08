using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System;

namespace Tron
{
	[System.Serializable]
	public struct Sensor
	{
		public Transform Transform;
		public float HitThreshold;
	}

	public struct Position
	{
		public float x;
		public float y;
	}

	public enum AgentMode
	{
		Training,
		Inferencing
	}

	public enum Direction
	{
		Up = 0,
		Down = 1,
		Right = 2,
		Left = 3
	}

	public class TronAgent : Agent
	{
		public int actionCount = 0;
		public GameObject enemy;
		public Position enemyPosition;
		public float canDetectEnemyFrom;
		public bool trustTower = true;
		public bool isMyenemyLocationValid = true;
		
		#region Steering
		[Header("Steering")]
		public KeyCode upKey;
		public KeyCode downKey;
		public KeyCode rightKey;
		public KeyCode leftKey;
		#endregion
		
		#region Attributes
		[Header("Agent atributes")]
		public float speed = 16;
		public bool isAgentAlive = true;
		public Direction direction;
		public Direction lastDirection;
		public GameObject wallPrefab;
		Collider2D wall;
		Vector2 lastWallEnd;
		public string tag;
		#endregion

		#region Training Modes
		[Tooltip("Are we training the agent or is the agent production ready?")]
		public AgentMode Mode = AgentMode.Training;
		#endregion

		#region Senses
		[Header("Observation Params")]
		[Tooltip("Sensors contain ray information to sense out the world, you can have as many sensors as you need.")]
		public Sensor[] Sensors;
		#endregion

		#region Rewards
		[Header("Rewards")]
		[Tooltip("What penatly is given when the agent crashes?")]
		public float HitPenalty;
		public float CloseCallPenalty;
		public float stayingAliveReward;
		public float rewardForKill;
		#endregion


		public override void OnActionReceived(float[] vectorAction)
		{
			base.OnActionReceived(vectorAction);
			print(name + " received action: go " + ((Direction)((int)vectorAction[0])).ToString());
			direction = (Direction)(int)vectorAction[0];
			actionCount++;
		}

		public void moveAgent(Direction dir)
		{
			switch (dir)
			{
				case Direction.Up:
					GetComponent<Rigidbody2D>().velocity = Vector2.up * speed;
					break;
				case Direction.Down: //DOWN
					GetComponent<Rigidbody2D>().velocity = -Vector2.up * speed;
					break;
				case Direction.Right: //RIGHT
					GetComponent<Rigidbody2D>().velocity = Vector2.right * speed;
					break;
				case Direction.Left: //LEFT
					GetComponent<Rigidbody2D>().velocity = -Vector2.right * speed;
					break;
			}

			direction = dir;
			spawnWall();
		}

		public override void Heuristic(float[] actionsOut)
		{
			actionsOut[0] = Input.GetKey(upKey) ? 0.0f : actionsOut[0];
			actionsOut[0] = Input.GetKey(downKey) ? 1.0f : actionsOut[0];
			actionsOut[0] = Input.GetKey(rightKey) ? 2.0f : actionsOut[0];
			actionsOut[0] = Input.GetKey(leftKey) ? 3.0f : actionsOut[0];
			direction = (Direction)((int)actionsOut[0]);
		}

		void changeWallDueToPlyerMove(Collider2D co, Vector2 a, Vector2 b)
		{
			if (co == null) return;
			co.transform.position = a + (b - a) * 0.5f;

			float dist = Vector2.Distance(a, b);
			if (a.x != b.x)
			{
				co.transform.localScale = new Vector2(dist + 1, 1);
			}
			else
				co.transform.localScale = new Vector2(1, dist + 1);
		}

		private void OnTriggerEnter2D(Collider2D co)
		{
			if (co != wall)
			{
				if(Mode == AgentMode.Inferencing)
				{
					KillPlayer();
				}
				else
				{
					print("Player lost: " + name);
					AddReward(HitPenalty);
					if (co.tag != tag && co.tag != "Wall") enemy.GetComponent<TronAgent>().killedAPlayer(); //informing enemy that they were killed by him
					EndEpisode();
					ResetAgent();
				}
			}
		}

		void KillPlayer()
		{
			print("Player lost: " + name);
			Destroy(gameObject);
		}
		void spawnWall()
		{
			lastWallEnd = transform.position;
			GameObject g = Instantiate(wallPrefab, transform.position, Quaternion.identity);
			wall = g.GetComponent<Collider2D>();
		}

		public override void OnEpisodeBegin()
		{
			//GetComponent<Rigidbody2D>().velocity = Vector2.up * speed;
			//direction = 0;
			//lastDirection = 0;
			//moveAgent(direction);
			ResetAgent();
		}

		private void LateUpdate()
		{
			float bonus = (actionCount / 1000 * 5);
			changeWallDueToPlyerMove(wall, lastWallEnd, transform.position);
			 if (checkIfOppositeDirections(direction, lastDirection))
			{
				if (Mode == AgentMode.Inferencing) KillPlayer();
				else {
					print("Player lost: " + name);
					//AddReward(HitPenalty);
					EndEpisode();
					ResetAgent();
				}
			}
			else if( lastDirection != direction){
				moveAgent(direction);
				lastDirection = direction;
				AddReward(bonus);
			}

			changeWallDueToPlyerMove(wall, lastWallEnd, transform.position);

			//AddReward(stayingAliveReward);
			AddReward(actionCount / 1000);
		}

		public bool checkIfOppositeDirections(Direction dir1, Direction dir2)
		{
			if ((dir1 == Direction.Left && dir2 == Direction.Right) ||
				(dir1 == Direction.Right && dir2 == Direction.Left) ||
				(dir1 == Direction.Up && dir2 == Direction.Down) ||
				(dir1 == Direction.Down && dir2 == Direction.Up)) return true;
			else
			{
				return false;
			}
		}

		public override void CollectObservations(VectorSensor sensor)
		{
			getEnemyPosition();
			sensor.AddObservation(enemyPosition.x);
			sensor.AddObservation(enemyPosition.y);
			sensor.AddObservation(transform.position);
			bool didHit = false;
			for (int i = 0; i < Sensors.Length; i++)
			{
				int layerMask = ~(LayerMask.GetMask("Ignore Raycast"));
				var current = Sensors[i];
				var xform = current.Transform;
				RaycastHit2D hitInfo = new RaycastHit2D();
				hitInfo = Physics2D.Raycast(current.Transform.position + new Vector3(0.0f, 0.0f, 0.0f), xform.up, 20, layerMask);;

				sensor.AddObservation(hitInfo);
				if (hitInfo.collider != null && hitInfo.collider != wall && hitInfo.distance < current.HitThreshold)
				{
					//AddReward(CloseCallPenalty);
					didHit = true;
				}
				else
				{
					
				}
			}
			if (didHit)
			{
				//ResetAgent();
			}
		}

		public void ResetAgent()
		{
			float min = -60;
			float max = 60;
			float x = UnityEngine.Random.Range(min, max);
			float y = UnityEngine.Random.Range(min, max);
			transform.position = new Vector2(x, y);
			int dir = UnityEngine.Random.Range(0, 3);
			direction = (Direction)dir;
			lastDirection = (Direction)dir;
			DestroyAllObjectsWithTag(tag);
			wall = null;
			lastWallEnd = new Vector2(x, y);
			moveAgent(direction);
		}

		public void DestroyAllObjectsWithTag(string tag)
		{
			var gameObjects = GameObject.FindGameObjectsWithTag(tag);

			for (var i = 0; i < gameObjects.Length; i++)
			{
				Destroy(gameObjects[i]);
			}
		}

		public void getEnemyPosition()
		{
			Position enemyPosition = new Position();
			enemyPosition.x = enemy.transform.position.x;
			enemyPosition.y = enemy.transform.position.y;

			Position myPosition = new Position();
			myPosition.x = GetComponent<Rigidbody2D>().transform.position.x;
			myPosition.y = GetComponent<Rigidbody2D>().transform.position.y;

			if(calculateDistance(enemyPosition, myPosition) < canDetectEnemyFrom)
			{
				this.enemyPosition = enemyPosition;
				isMyenemyLocationValid = true;
			}
			else
			{
				isMyenemyLocationValid = false;
			}
		}

		public double calculateDistance(Position one, Position second)
		{
			return Math.Sqrt(Math.Pow(second.x - one.x, 2) + Math.Pow(second.y - one.y, 2));
		}

		public void killedAPlayer()
		{
			AddReward(rewardForKill);
		}

		public void receiveHelp(float x, float y)
		{
			Position receivedEnemyPosition = new Position();
			receivedEnemyPosition.x = x;
			receivedEnemyPosition.y = y;

			Position myPosition = new Position();
			myPosition.x = GetComponent<Rigidbody2D>().transform.position.x;
			myPosition.y = GetComponent<Rigidbody2D>().transform.position.y;

			if (isMyenemyLocationValid && (enemyPosition.x != receivedEnemyPosition.x || enemyPosition.y != receivedEnemyPosition.y))
			{
				trustTower = false;
			}

			if (trustTower)
			{
				enemyPosition = receivedEnemyPosition;
			}
		}
	}
}
