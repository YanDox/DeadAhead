using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class animtracker : MonoBehaviour
{
	public Animator anim;
	void Update()
	{
		if (anim.GetCurrentAnimatorStateInfo(0).IsName("end") && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
		{
			SceneManager.LoadScene(0);
		}
	}
}
