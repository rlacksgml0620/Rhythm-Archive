using UnityEngine;

namespace RhythmArchive
{
    /// <summary>노트 하나의 그래픽(머리 + 롱노트 몸통 + 꼬리). 오브젝트 풀에서 재사용된다.</summary>
    public class NoteView
    {
        public readonly GameObject Root;
        readonly SpriteRenderer head;
        readonly SpriteRenderer body;
        readonly SpriteRenderer tail;

        public NoteView(Transform parent)
        {
            Root = new GameObject("Note");
            Root.transform.SetParent(parent, false);
            body = Make("Body", 4);
            tail = Make("Tail", 5);
            head = Make("Head", 5);
        }

        SpriteRenderer Make(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprites.Rounded;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = order;
            return sr;
        }

        public void SetActive(bool active)
        {
            if (Root.activeSelf != active) Root.SetActive(active);
        }

        /// <param name="dim">미스/끊긴 롱노트는 어둡게 표시</param>
        public void Layout(float x, float laneWidth, float headY, float tailY, bool isHold, Color color, bool dim)
        {
            float w = laneWidth * 0.9f;
            float h = Playfield.NoteHeight;
            if (dim) color = Color.Lerp(color, new Color(0.3f, 0.3f, 0.35f), 0.7f);

            head.transform.position = new Vector3(x, headY, 0);
            head.size = new Vector2(w, h);
            head.color = color;

            body.enabled = isHold;
            tail.enabled = isHold;
            if (!isHold) return;

            float len = Mathf.Max(0f, tailY - headY);
            body.transform.position = new Vector3(x, headY + len / 2f, 0);
            body.size = new Vector2(laneWidth * 0.62f, Mathf.Max(h, len));
            var bc = color;
            bc.a = dim ? 0.3f : 0.5f;
            body.color = bc;

            tail.transform.position = new Vector3(x, tailY, 0);
            tail.size = new Vector2(w * 0.8f, h * 0.55f);
            var tc = color;
            tc.a = dim ? 0.5f : 0.85f;
            tail.color = tc;
        }
    }
}
