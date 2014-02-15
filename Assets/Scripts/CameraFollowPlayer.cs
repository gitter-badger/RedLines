﻿using UnityEngine;
using System.Collections;

public class CameraFollowPlayer : MonoBehaviour 
{
	public Vector3 offset;
	private Transform player;
	
	
	void Start () 
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
	}
	
	
	void FixedUpdate () 
	{
		if (Player.isDead)
		{
			transform.LookAt(player.position);
		}
		else
		{
			Vector3 playerPosition = player.position;
			transform.position = playerPosition + offset;
		}

	}
	
}
