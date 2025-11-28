using UnityEngine;

public class Parallex : MonoBehaviour {

	private float length, startPos;
    private float height, startPosY;
	public GameObject cam;
	public float parallexEffect;
    public float parallexEffectY;    // set to 0 to disable vertical parallax

	void Start () {
		startPos = transform.position.x;
		length = GetComponent<SpriteRenderer>().bounds.size.x;

        startPosY = transform.position.y;
        height = GetComponent<SpriteRenderer>().bounds.size.y;
	}
	
	void FixedUpdate () {
		float temp = (cam.transform.position.x * (1-parallexEffect));
		float dist = (cam.transform.position.x * parallexEffect);

        float tempY = (cam.transform.position.y * (1-parallexEffectY));
        float distY = (cam.transform.position.y * parallexEffectY);

		transform.position = new Vector3(startPos + dist, startPosY + distY, transform.position.z);

		if      (temp > startPos + length) startPos += length;
		else if (temp < startPos - length) startPos -= length;
	}

}
