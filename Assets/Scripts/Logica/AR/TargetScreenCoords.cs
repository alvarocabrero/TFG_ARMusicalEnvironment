using UnityEngine;
using System.Collections;
using Vuforia;
using System;

public class TargetScreenCoords : MonoBehaviour
{
	private ImageTargetBehaviour mImageTargetBehaviour = null;
	
	private AudioSource audioSource;
	private int inicial;

	private bool actualStatus;
	private int espera = 0;

	private Vector3 anguloInicial_F;
	private Vector3 anguloInicial_E;
	private Vector3 anguloInicial_U;
	private Quaternion anguloInicial_Q;
	private Vector3 anguloInicial_R;
	private Vector3 anguloActual_F;
	private Vector3 anguloActual_E;
	private Vector3 anguloActual_U;
	private Quaternion anguloActual_Q;
	private Vector3 anguloActual_R;
	// Use this for initialization
	void Start()
	{
		inicial = 0;
		
		actualStatus = false;

		// We retrieve the ImageTargetBehaviour component
		// Note: This only works if this script is attached to an ImageTarget
		mImageTargetBehaviour = GetComponent<ImageTargetBehaviour>();
		audioSource = GetComponent<AudioSource>();
		

		if (mImageTargetBehaviour == null)
		{
			Debug.Log("ImageTargetBehaviour not found ");
		}
	}

	// Update is called once per frame
	void Update()
	{

		//Debug.Log("Espera:" +espera);
		if (mImageTargetBehaviour == null)
		{
			Debug.Log("ImageTargetBehaviour not found");
			return;
		}
		if (actualStatus)
		{
			espera++;
			if (espera > 200)
			{
				if (inicial == 0)
				{
					
					anguloInicial_F = mImageTargetBehaviour.transform.forward;

					anguloInicial_U = mImageTargetBehaviour.transform.up;

					anguloInicial_R = mImageTargetBehaviour.transform.right;

					anguloInicial_E = mImageTargetBehaviour.transform.eulerAngles;

					anguloInicial_Q = mImageTargetBehaviour.transform.rotation;
					
					inicial = 1;
				}
				Debug.Log(mImageTargetBehaviour.transform.rotation);
				anguloActual_F = mImageTargetBehaviour.transform.forward;

				anguloActual_U = mImageTargetBehaviour.transform.up;

				anguloActual_R = mImageTargetBehaviour.transform.right;

				anguloActual_E = mImageTargetBehaviour.transform.eulerAngles;

				anguloActual_Q = mImageTargetBehaviour.transform.rotation;

				//diferencia entre angulos
				float angle_f = Vector3.Angle(anguloInicial_F, anguloActual_F);
				float angle_e = Vector3.Angle(anguloInicial_E, anguloActual_E);
				float angle_q = Quaternion.Angle(anguloInicial_Q, anguloActual_Q);
				float angle_u = Vector3.Angle(anguloInicial_U, anguloActual_U);
				float angle_r = Vector3.Angle(anguloInicial_R, anguloActual_R);

				Debug.Log("QUARTENION:"+ angle_q + " QUARTENION_INICIAL: " + anguloInicial_Q + " QUARTENION_ACTUAL: "+ anguloActual_Q);
				Debug.Log("FORWARD: "+ angle_f + " FORWARD_INICIAL: " + anguloInicial_F + " FORWARD_ACTUAL: " + anguloActual_F);
				Debug.Log("UP: " + angle_u + " UP_INICIAL: " + anguloInicial_U + " UP_ACTUAL: " + anguloActual_U);
				Debug.Log("RIGHT: " + angle_r + " RIGHT_INICIAL: " + anguloInicial_R + " RIGHT_ACTUAL: " + anguloActual_R);
				Debug.Log("EULER: " + angle_e + " EULER_INICIAL: " + anguloInicial_E + " EULER_ACTUAL: " + anguloActual_E);
				
				//Debug.Log("ImageTargetBehaviour ENABLED");

				screenCoordinates();
			}
		}
		else
		{
			inicial = 0;
			espera = 0;
			//Debug.Log("ImageTargetBehaviour DISABLE");
		}
		Audio();
	}


	private void screenCoordinates()
	{
		if (mImageTargetBehaviour == null)
		{
			Debug.Log("ImageTargetBehaviour not found");
			return;
		}

		Vector2 targetSize = mImageTargetBehaviour.GetSize();
		float targetAspect = targetSize.x / targetSize.y;

		// We define a point in the target local reference 
		// we take the bottom-left corner of the target, 
		// just as an example
		// Note: the target reference plane in Unity is X-Z, 
		// while Y is the normal direction to the target plane
		Vector3 pointOnTarget = new Vector3(-0.5f, 0, -0.5f / targetAspect);

		// We convert the local point to world coordinates
		Vector3 targetPointInWorldRef = transform.TransformPoint(pointOnTarget);

		Debug.Log("target point in World Ref: " + targetPointInWorldRef.x + ", " + targetPointInWorldRef.y);

		// We project the world coordinates to screen coords (pixels)
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(targetPointInWorldRef);

		Debug.Log("target point in screen coords: " + screenPoint.x + ", " + screenPoint.y);
	}



	public void changeStatus()
	{
		actualStatus = !actualStatus;
	}

	private void Audio()
	{
		if (audioSource == null)
			return;

	/*	
		if (anguloActual - anguloInicial >v.euler)
		{
			Debug.Log("Baja volumen");
			this.audioSource.volume -= 0.5f;
			anguloInicial = mImageTargetBehaviour.transform.rotation.eulerAngles;
		}
		*/
		
	}

}

