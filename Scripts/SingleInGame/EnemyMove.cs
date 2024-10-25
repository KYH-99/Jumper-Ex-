using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    Rigidbody2D rigid;
    public int nextMove;
    Animator anim;
    SpriteRenderer spriteRenderer;
    CapsuleCollider2D capsulecollider;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        capsulecollider = GetComponent<CapsuleCollider2D>();
        aiMove();

        Invoke("aiMove",5);
    }

    void FixedUpdate()
    {
        //기본움직임
        rigid.velocity = new Vector2(nextMove, rigid.velocity.y);
        
        //플랫폼 체크
        Vector2 frontVec = new Vector2(rigid.position.x+ nextMove*0.3f, rigid.position.y);
        Debug.DrawRay(frontVec, Vector3.down, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, 1, LayerMask.GetMask("Platform"));
        if (rayHit.collider == null)
            Turn();
    }

    //재귀함수
    void aiMove()
    {
        //다음활동
        nextMove = Random.Range(-1, 2);

        //스프라이트 애니메이션
        anim.SetInteger("WalkSpeed", nextMove);

        //방향전환
        if(nextMove != 0)
        spriteRenderer.flipX = nextMove == 1;

        //재귀활동
        float nextThinkTime = Random.Range(2f, 5f);
        Invoke("aiMove",nextThinkTime);
    }

    void Turn()
    {
        nextMove = nextMove * -1;
        spriteRenderer.flipX = nextMove == 1;

        CancelInvoke();
        Invoke("aiMove", 5);
    }

    public void OnDamaged()
    {
        //Sprite Alpha
        spriteRenderer.color = new Color(1,1,1,0.4f);
        //Sprite Flip Y
        spriteRenderer.flipY = true;
        //Collider Disable
        capsulecollider.enabled = false;
        //Die Effect Jump
        rigid.AddForce(Vector2.up*5, ForceMode2D.Impulse);
        //Destroy
        Invoke("DeActive",5);
    }

    void DeActive()
    {
        gameObject.SetActive(false);
    }
}

