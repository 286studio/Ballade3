using Naninovel;
using UnityEngine;

// 这是AVG部分的GameManager
public class GameManager : MonoBehaviour
{
    // https://naninovel.com/guide/integration-options.html?#manual-initialization
    private async void Start()
    {
        await RuntimeInitializer.InitializeAsync();
    }
}
