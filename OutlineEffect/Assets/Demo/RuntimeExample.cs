using UnityEngine;
using System.Collections;

public class RuntimeExample : MonoBehaviour {

    float timer = 0;
    public Renderer renderer;
    bool ping = false;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        timer += Time.deltaTime;
        if (timer > 2)
        {
            ping = !ping;

            if (ping)
                Camera.main.GetComponent<OutlineEffect>().outlineRenderers.Add(renderer);
            else
                Camera.main.GetComponent<OutlineEffect>().outlineRenderers.Remove(renderer);

            timer = 0;
        }
        

    }
}
