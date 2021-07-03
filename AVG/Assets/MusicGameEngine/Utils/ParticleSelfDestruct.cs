
using UnityEngine;

public class ParticleSelfDestruct : MonoBehaviour
{
    ParticleSystem ps;
    bool playing = false;
    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!playing && ps.isPlaying) playing = true;
        if (playing)
        {
            if (ps.isStopped) Destroy(gameObject);
        }
    }
}
