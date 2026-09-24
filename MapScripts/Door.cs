using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isOpen = false;
    public Collider2D doorCollider;
    public SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (doorCollider == null)
            doorCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void Open()
    {
        isOpen = true;
        if (doorCollider != null)
            doorCollider.enabled = true;
        if (spriteRenderer != null)
            spriteRenderer.enabled = true; //보이게
    }
    public void CloseHidden()
    {
        isOpen = false;

        if (doorCollider != null)
            doorCollider.enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false; //안 보이게
    }
}