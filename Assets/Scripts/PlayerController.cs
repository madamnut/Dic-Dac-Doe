using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        Debug.Log($"[Start] OwnerClientId: {OwnerClientId}, IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");

        if (IsOwner)
        {
            // 색상 및 시작 위치
            if (IsHost)
            {
                spriteRenderer.color = Color.blue;
                transform.position = new Vector3(-3f, 0f, 0f);
                Debug.Log("[Start] Host: 위치 (-3, 0)");
            }
            else
            {
                spriteRenderer.color = Color.red;
                transform.position = new Vector3(3f, 0f, 0f);
                Debug.Log("[Start] Client: 위치 (3, 0)");
            }
        }
        else
        {
            spriteRenderer.color = Color.gray;
            Debug.Log("[Start] Not Owner: 회색 처리");
        }
    }

    private void Update()
    {
        Debug.Log($"[Update] ClientId: {OwnerClientId} | IsOwner: {IsOwner} | IsLocalPlayer: {IsLocalPlayer} | IsClient: {IsClient} | IsServer: {IsServer}");

        if (!IsOwner)
        {
            Debug.Log("[Update] 조종 불가: Not Owner");
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Debug.Log($"[Input] H: {h}, V: {v}");

        Vector3 move = new Vector3(h, v, 0f) * moveSpeed * Time.deltaTime;
        Debug.Log($"[Move] Vector: {move}");

        transform.Translate(move);
        Debug.Log($"[Position] Updated to: {transform.position}");
    }
}
