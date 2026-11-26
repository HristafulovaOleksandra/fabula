import json
import sys
import urllib.error
import urllib.request
import uuid

API = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080/api"
PASSWORD = "fabula-demo-2026"
AUTHOR = {"email": "demo.author@fabula.local", "username": "demo_author", "displayName": "Demo Author", "role": "Author"}
STUDENT = {"email": "demo.student@fabula.local", "username": "demo_student", "displayName": "Demo Student", "role": "Student"}
TITLE = "Lost at the Station"
NARRATOR = ""
JPEG_SIGNATURE = bytes([0xFF, 0xD8, 0xFF])

BACKGROUNDS = {
    "Station hall": ("https://pd.w.org/2025/01/5476796735e2ae652.05490864-1536x1024.jpg", "Nilo Velez", "https://wordpress.org/photos/photo/5476796735/"),
    "Cafe": ("https://pd.w.org/2026/06/5386a305663ca3502.07887809-1536x1157.jpg", "Iyundhu Kennedy", "https://wordpress.org/photos/photo/5386a30566/"),
    "Station interior": ("https://pd.w.org/2024/02/10765c3d08bccce08.12231948-1536x1021.jpg", "Nilo Velez", "https://wordpress.org/photos/photo/10765c3d08/"),
    "Busy platform": ("https://pd.w.org/2025/08/772688c3b568e7059.86321544-1536x1152.jpg", "Mehraz Morshed", "https://wordpress.org/photos/photo/772688c3b5/"),
    "Empty platform": ("https://pd.w.org/2025/08/255688f3e96428b93.97080051-1536x918.jpg", "Md Limon Miah", "https://wordpress.org/photos/photo/255688f3e9/"),
    "Train carriage": ("https://pd.w.org/2025/01/58678ab04ea3ae35.73912990-1536x1024.jpg", "Nilo Velez", "https://wordpress.org/photos/photo/58678ab04e/"),
}

STORY = {
    "title": TITLE,
    "description": "Anna wants to visit her grandmother in Green Hill. Read carefully: if you understand the people at the station, she will catch her train.",
    "level": "A2",
    "priceCrystals": 0,
    "tagIds": [1, 8],
    "words": [
        {"word": "ticket", "translation": "квиток"},
        {"word": "platform", "translation": "платформа"},
        {"word": "train", "translation": "потяг"},
        {"word": "leave", "translation": "відправлятися, їхати"},
        {"word": "late", "translation": "пізно; запізнитися"},
        {"word": "return", "translation": "зворотний (квиток туди й назад)"},
        {"word": "hurry", "translation": "поспішати"},
        {"word": "arrive", "translation": "прибувати"},
    ],
}

ANNA = {"position": "Left", "name": "Anna", "emotion": "neutral"}

SCENES = [
    {
        "title": "Arrival",
        "background": "Station hall",
        "characters": [ANNA, {"position": "Right", "name": "Guard", "emotion": "neutral"}],
        "lines": [
            (NARRATOR, "Anna arrives at a big station. She wants to visit her grandmother in Green Hill."),
            ("Anna", "Excuse me, I need a train to Green Hill. Where can I buy a ticket?"),
            ("Guard", "At the ticket office, next to the cafe. The train to Green Hill leaves at ten o'clock."),
            ("Guard", "It's half past nine now, so don't be late!"),
        ],
        "choices": [("Go to the ticket office", "Ticket office"), ("Buy a coffee first, there is a lot of time", "Cafe")],
    },
    {
        "title": "Cafe",
        "background": "Cafe",
        "characters": [ANNA, {"position": "Right", "name": "Waiter", "emotion": "neutral"}],
        "lines": [
            (NARRATOR, "Anna buys a coffee and sits by the window. The cafe is warm and quiet."),
            ("Waiter", "Are you waiting for a train?"),
            ("Anna", "Yes, the ten o'clock train to Green Hill. I don't have a ticket yet."),
            ("Waiter", "Ten o'clock? It's ten to ten now! You need to hurry."),
        ],
        "choices": [("Run to the ticket office", "Ticket office")],
    },
    {
        "title": "Ticket office",
        "background": "Station interior",
        "characters": [ANNA, {"position": "Right", "name": "Clerk", "emotion": "neutral"}],
        "lines": [
            ("Clerk", "Good morning! Where are you going?"),
            ("Anna", "To Green Hill, please. I'm coming back on Sunday."),
            ("Clerk", "Then you need a return ticket. That's twelve pounds."),
            (NARRATOR, "Anna pays and takes her ticket."),
            ("Clerk", "Your train leaves in ten minutes from platform two."),
        ],
        "choices": [("Go to platform two", "Platform two"), ("Go to platform ten", "Wrong platform")],
    },
    {
        "title": "Wrong platform",
        "background": "Busy platform",
        "characters": [ANNA, {"position": "Right", "name": "Old man", "emotion": "neutral"}],
        "lines": [
            (NARRATOR, "There is a train on platform ten, but it looks different."),
            ("Anna", "Excuse me, is this the train to Green Hill?"),
            ("Old man", "No, dear. This train goes to the sea."),
            ("Old man", "Trains to Green Hill leave from platform two. Hurry, it isn't far!"),
        ],
        "choices": [("Run to platform two", "Platform two")],
    },
    {
        "title": "Platform two",
        "background": "Empty platform",
        "characters": [ANNA],
        "lines": [
            (NARRATOR, "Anna runs to platform two. The train is still there!"),
            ("Anna", "Phew! I'm not late."),
        ],
        "choices": [("Get on the train", "On the train")],
    },
    {
        "title": "On the train",
        "background": "Train carriage",
        "characters": [ANNA, {"position": "Right", "name": "Conductor", "emotion": "happy"}],
        "lines": [
            ("Conductor", "Tickets, please!"),
            ("Anna", "Here you are. It's a return ticket to Green Hill."),
            ("Conductor", "Thank you. We arrive in one hour. Have a nice trip!"),
            (NARRATOR, "Anna looks out of the window and smiles. Grandma is waiting for her."),
        ],
        "choices": [],
    },
]


def call(method, path, body=None, token=None):
    data = json.dumps(body).encode() if body is not None else None
    request = urllib.request.Request(API + path, data=data, method=method)
    if data is not None:
        request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(request) as response:
            text = response.read()
            return response.status, json.loads(text) if text else None
    except urllib.error.HTTPError as error:
        text = error.read()
        return error.code, json.loads(text) if text else None


def download_image(url):
    request = urllib.request.Request(url, headers={"User-Agent": "fabula-demo-seed"})
    with urllib.request.urlopen(request, timeout=60) as response:
        content = response.read()
    if not content.startswith(JPEG_SIGNATURE) or len(content) > 5 * 1024 * 1024:
        raise SystemExit(f"Неочікуваний файл: {url}")
    return content


def upload_background(token, name, content):
    boundary = "----fabula" + uuid.uuid4().hex
    crlf = "\r\n"
    head = (f"--{boundary}{crlf}Content-Disposition: form-data; name=\"name\"{crlf}{crlf}{name}{crlf}"
            f"--{boundary}{crlf}Content-Disposition: form-data; name=\"file\"; filename=\"background.jpg\"{crlf}"
            f"Content-Type: image/jpeg{crlf}{crlf}")
    body = head.encode() + content + f"{crlf}--{boundary}--{crlf}".encode()
    request = urllib.request.Request(API + "/backgrounds", data=body, method="POST")
    request.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
    request.add_header("Authorization", "Bearer " + token)
    with urllib.request.urlopen(request) as response:
        return json.loads(response.read())["id"]


def ensure_backgrounds(token):
    status, existing = call("GET", "/backgrounds", token=token)
    by_name = {item["name"]: item["id"] for item in existing or []}
    ids = {}
    for name, (url, _author, _page) in BACKGROUNDS.items():
        ids[name] = by_name.get(name) or upload_background(token, name, download_image(url))
    return ids


def account(profile):
    status, body = call("POST", "/auth/login", {"email": profile["email"], "password": PASSWORD})
    if status == 200:
        return body["token"]
    status, body = call("POST", "/auth/register", dict(profile, password=PASSWORD))
    if status != 200:
        raise SystemExit(f"Не вдалося створити {profile['username']}: {status} {body}")
    return body["token"]


def main():
    author = account(AUTHOR)
    account(STUDENT)

    status, mine = call("GET", "/stories?mine=true", token=author)
    for story in mine or []:
        if story["title"] == TITLE:
            call("DELETE", f"/stories/{story['id']}", token=author)

    backgrounds = ensure_backgrounds(author)
    status, story = call("POST", "/stories", STORY, token=author)
    story_id = story["id"]
    cast = {}
    for scene in SCENES:
        for character in scene["characters"]:
            if character["name"] not in cast:
                status, created = call("POST", f"/stories/{story_id}/characters", {"name": character["name"]}, token=author)
                cast[character["name"]] = created["id"]

    scene_ids = {}
    for scene in SCENES:
        status, created = call("POST", f"/stories/{story_id}/scenes", {"title": scene["title"]}, token=author)
        scene_ids[scene["title"]] = created["id"]

    for scene in SCENES:
        payload = {
            "title": scene["title"],
            "backgroundId": backgrounds[scene["background"]],
            "characters": [
                {"position": c["position"], "storyCharacterId": cast[c["name"]], "emotion": c["emotion"]} for c in scene["characters"]
            ],
            "lines": [{"speaker": speaker or None, "text": text} for speaker, text in scene["lines"]],
            "choices": [{"text": text, "nextSceneId": scene_ids[target], "priceCrystals": 0} for text, target in scene["choices"]],
        }
        status, saved = call("PUT", f"/stories/{story_id}/scenes/{scene_ids[scene['title']]}", payload, token=author)
        if status != 200:
            raise SystemExit(f"Сцена {scene['title']}: {status} {saved}")

    status, published = call("POST", f"/stories/{story_id}/publish", token=author)
    print(f"Демо-історію опубліковано: {published['title']} ({story_id}), сцен: {len(SCENES)}")


if __name__ == "__main__":
    main()
