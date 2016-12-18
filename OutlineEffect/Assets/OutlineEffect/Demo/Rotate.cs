using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour
{
    float timer;
    const float time = 1;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        transform.RotateAround(Vector3.up, Time.deltaTime);

        timer -= Time.deltaTime;
        if(timer < 0)
        {
            timer = time;
            //GetComponent<Outline>().enabled = !GetComponent<Outline>().enabled;
        }
    }
}
