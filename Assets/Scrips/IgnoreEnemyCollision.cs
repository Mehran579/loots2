using UnityEngine;

public class IgnoreEnemyCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    [Tooltip("The name of the layer you want to ignore.")]
    [SerializeField] private string targetLayerName = "Enemy";

    private void Start()
    {
        // Get the integer index of the Enemy layer (e.g., layer 3, layer 6, etc.)
        int enemyLayerIndex = LayerMask.NameToLayer(targetLayerName);

        // Get the current layer index of this specific GameObject
        int myLayerIndex = gameObject.layer;

        // Check to ensure the target layer actually exists in your project settings
        if (enemyLayerIndex == -1)
        {
            Debug.LogError($"Layer '{targetLayerName}' does not exist! Please check your spelling and Unity Project Settings.");
            return;
        }

        // Globally tell Unity's 2D Physics engine to ignore collisions between these two layers
        Physics2D.IgnoreLayerCollision(myLayerIndex, enemyLayerIndex, true);

        Debug.Log($"Physics2D: Successfully ignored all collisions between layer '{LayerMask.LayerToName(myLayerIndex)}' and '{targetLayerName}'.");
    }
}
