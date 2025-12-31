using UnityEngine;

public class PlayerView : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    //Presenter가 시키는 일만 합니다.
    public void PlayAnimation(string animName)
    {
        anim.Play(animName);
    }

    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    public void Flip(float direction)
    {
        if (direction != 0)
        {
            transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
        }
    }

    public void SetTrigger(string triggerName)
    {
        anim.SetTrigger(triggerName);
    }

    public void SetFloat(string name, float value) => anim.SetFloat(name, value);
    public void SetBool(string name, bool value) => anim.SetBool(name, value);
}