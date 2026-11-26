import json
import re
import sys
import time
import urllib.parse
import struct
import subprocess
import urllib.request
import uuid
import zlib

B = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080/api"
failures = []
checks = 0


def call(method, path, body=None, token=None, raw=None, ctype="application/json"):
    status, payload = send(method, path, body, token, raw, ctype)
    if status == 429:
        print("…  ліміт частоти запитів, чекаємо хвилину")
        time.sleep(61)
        status, payload = send(method, path, body, token, raw, ctype)
    return status, payload


def send(method, path, body=None, token=None, raw=None, ctype="application/json"):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(B + path, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", ctype)
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            t = r.read()
            return r.status, (json.loads(t) if t else None)
    except urllib.error.HTTPError as e:
        t = e.read()
        try:
            return e.code, (json.loads(t) if t else None)
        except json.JSONDecodeError:
            return e.code, t.decode(errors="replace")[:300]


def ok(label, cond):
    global checks
    checks += 1
    print(("OK   " if cond else "FAIL ") + label)
    if not cond:
        failures.append(label)


def png():
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 1, 1, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(b"\x00\xff\x00\x00")) + chunk(b"IEND", b""))


sfx = uuid.uuid4().hex[:6]
bnd = "----fabula" + sfx


def form(fields, fname, content):
    body = b""
    for k, v in fields.items():
        body += f"--{bnd}\r\nContent-Disposition: form-data; name=\"{k}\"\r\n\r\n{v}\r\n".encode()
    body += (f"--{bnd}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fname}\"\r\n"
             f"Content-Type: image/png\r\n\r\n").encode() + content + f"\r\n--{bnd}--\r\n".encode()
    return body


mp = f"multipart/form-data; boundary={bnd}"
s, author = call("POST", "/auth/register", {"email": f"author{sfx}@test.local", "username": f"author_{sfx}", "password": "secret-pass-1", "displayName": "Test Author", "role": "Author"})
ok(f"реєстрація автора ({s})", s == 200 and author["user"]["role"] == "Author")
s, _ = call("POST", "/auth/register", {"email": f"author{sfx}@test.local", "username": f"author_{sfx}", "password": "secret-pass-1", "displayName": "Second", "role": "Author"})
ok(f"повторна пошта відхилена ({s})", s == 409)
s, _ = call("POST", "/auth/register", {"email": f"adm{sfx}@test.local", "username": f"adm_{sfx}", "password": "secret-pass-1", "displayName": "Admin", "role": "Admin"})
ok(f"самореєстрація адміністратора заборонена ({s})", s == 400)
s, _ = call("POST", "/auth/register", {"email": "not-an-email", "username": "a b", "password": "123", "displayName": "X"})
ok(f"неправильні дані реєстрації відхилено ({s})", s == 400)
s, _ = call("POST", "/auth/login", {"email": f"author{sfx}@test.local", "username": f"author_{sfx}", "password": "wrong-password"})
ok(f"неправильний пароль ({s})", s == 401)
s, login = call("POST", "/auth/login", {"email": f"author{sfx}@test.local", "username": f"author_{sfx}", "password": "secret-pass-1"})
tok = login["token"]
ok(f"вхід ({s})", s == 200)
s, _ = call("POST", "/auth/login", {"login": f"author_{sfx}", "password": "secret-pass-1"})
s2, _ = call("POST", "/auth/login", {"login": f"@Author_{sfx}", "password": "secret-pass-1"})
s3, _ = call("POST", "/auth/login", {"login": f"author_{sfx}", "password": "wrong-password"})
ok(f"вхід за ніком, з «@» і великими літерами; неправильний пароль відхилено ({s}, {s2}, {s3})", s == 200 and s2 == 200 and s3 == 401)
s, me = call("GET", "/auth/me", token=tok)
ok(f"/auth/me без хешу пароля ({s})", s == 200 and "passwordHash" not in json.dumps(me))
s, tags = call("GET", "/tags")
ok(f"теги ({s}, {len(tags)})", s == 200 and len(tags) == 19)
s, story = call("POST", "/stories", {"title": "Lost at the Station", "description": "Test", "level": "A2", "priceCrystals": 0,
                                     "tagIds": [1, 8], "words": [{"word": "ticket", "translation": "квиток"}, {"word": "platform", "translation": "платформа"}]}, token=tok)
sid = story["id"]
ok(f"створення історії ({s})", s == 201 and story["status"] == "Draft" and len(story["words"]) == 2)
s, tr = call("GET", "/dictionary?word=tickets", token=tok)
ok(f"переклад зі словника ({s}, {tr and tr['translations'][:1]})", s == 200 and tr["translations"][:1] == ["квиток"])
s, own = call("POST", "/tags", {"name": f"  smoke   tag {sfx} ", "kind": "Topic"}, token=tok)
ok(f"власний тег чекає на модерацію ({s})", s == 200 and own["status"] == "Pending" and own["name"] == f"Smoke tag {sfx}")
s, _ = call("POST", "/tags", {"name": "<b>x</b>", "kind": "Genre"}, token=tok)
ok(f"некоректну назву тегу відхилено ({s})", s == 400)
s, public_tags = call("GET", "/tags")
s2, available = call("GET", "/tags/available", token=tok)
ok("власний тег не видно публічно, але видно автору",
   all(t["id"] != own["id"] for t in public_tags) and any(t["id"] == own["id"] for t in available))
s, framed = call("PUT", f"/stories/{sid}", {"title": "Lost at the Station", "description": "Test", "level": "A2", "priceCrystals": 0,
                                            "tagIds": [1, 8, own["id"]], "words": story["words"],
                                            "coverFocusX": 30, "coverFocusY": 70, "coverZoom": 1.5}, token=tok)
ok(f"обкладинка й власний тег збережені ({s})",
   s == 200 and (framed["coverFocusX"], framed["coverFocusY"], framed["coverZoom"]) == (30, 70, 1.5) and any(t["id"] == own["id"] for t in framed["tags"]))
s, econ = call("GET", "/economy")
ok(f"ліміти й нагороди економіки з налаштувань ({s})",
   s == 200 and (econ["choicePriceMin"], econ["choicePriceMax"], econ["storyPriceMin"], econ["storyPriceMax"]) == (10, 30, 50, 150)
   and econ["storyRewards"]["A2"] == 12 and econ["dailyGoalReward"] == 5 and econ["streakFreezePrice"] == 20)
s, _ = call("PUT", f"/stories/{sid}", {"title": "X", "level": "A2", "priceCrystals": 40}, token=tok)
s2, _ = call("PUT", f"/stories/{sid}", {"title": "X", "level": "A2", "priceCrystals": 200}, token=tok)
ok(f"ціна історії поза 50–150 відхилена ({s}, {s2})", s == 400 and s2 == 400)
s, _ = call("PUT", f"/stories/{sid}", {"title": "X", "level": "A2", "priceCrystals": 0, "coverZoom": 5}, token=tok)
ok(f"масштаб обкладинки поза межами відхилено ({s})", s == 400)
s, shown = call("GET", "/stories")
ok("історія без публікації не видна в бібліотеці", all(x["id"] != sid for x in shown))
s, sc1 = call("POST", f"/stories/{sid}/scenes", {"title": "Arrival"}, token=tok)
s2, sc2 = call("POST", f"/stories/{sid}/scenes", {"title": "Ticket office"}, token=tok)
ok(f"створення двох сцен ({s}, {s2})", s == 201 and s2 == 201)
s, bg = call("POST", "/backgrounds", raw=form({"name": "Station"}, "station.png", png()), token=tok, ctype=mp)
ok(f"завантаження фону ({s})", s == 200 and bg["imagePath"].startswith("/uploads/backgrounds/"))
s, _ = call("POST", "/backgrounds", raw=form({"name": "Fake"}, "fake.png", b"not an image"), token=tok, ctype=mp)
ok(f"підроблений png відхилено ({s})", s == 400)
s, _ = call("POST", "/uploads/covers", raw=form({}, "evil.svg", b"<svg onload=alert(1)>"), token=tok, ctype=mp)
ok(f"svg відхилено ({s})", s == 400)
img = urllib.request.urlopen("http://localhost:5080" + bg["imagePath"])
ok(f"картинка віддається ({img.status})", img.status == 200)
s, anna = call("POST", f"/stories/{sid}/characters", {"name": "Anna"}, token=tok)
s2, guard = call("POST", f"/stories/{sid}/characters", {"name": "Guard"}, token=tok)
ok(f"персонажі історії створені ({s}, {s2})", s == 200 and s2 == 200)
s, _ = call("POST", f"/stories/{sid}/characters", {"name": "anna"}, token=tok)
ok(f"дубль імені персонажа відхилено ({s})", s == 400)
s, _ = call("PUT", f"/stories/{sid}/characters/{anna['id']}",
            {"name": "Anna", "states": [{"key": "neutral", "imagePath": "https://evil.example/x.png"}]}, token=tok)
ok(f"чужа картинка персонажа відхилена ({s})", s == 400)
s, up = call("POST", "/uploads/characters", raw=form({}, "anna.png", png()), token=tok, ctype=mp)
anna_img = up["path"]
s, anna = call("PUT", f"/stories/{sid}/characters/{anna['id']}",
               {"name": "Anna", "states": [{"key": "neutral", "imagePath": anna_img}, {"key": "angry", "imagePath": anna_img}]}, token=tok)
ok(f"власний стан персонажа збережено ({s})", s == 200 and [x["key"] for x in anna["states"]] == ["neutral", "angry"])
s, _ = call("PUT", f"/stories/{sid}/characters/{anna['id']}",
            {"name": "Anna", "states": [{"key": "angry", "imagePath": None}, {"key": "Angry", "imagePath": None}]}, token=tok)
ok(f"повтор назви стану відхилено ({s})", s == 400)
scene = {"title": "Arrival", "backgroundId": bg["id"],
         "characters": [{"position": "Left", "storyCharacterId": anna["id"], "emotion": "happy"}, {"position": "Right", "storyCharacterId": guard["id"]}],
         "lines": [{"speaker": "Anna", "text": "Excuse me, where can I buy a ticket?"},
                   {"speaker": "Guard", "text": "The ticket office is next to platform two."}],
         "choices": [{"text": "Go to the ticket office", "nextSceneId": sc2["id"], "priceCrystals": 0},
                     {"text": "Ask again", "priceCrystals": 10}]}
s, saved = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", scene, token=tok)
ok(f"збереження сцени одним запитом ({s})", s == 200 and len(saved["lines"]) == 2 and len(saved["choices"]) == 2 and saved["backgroundImagePath"])
bad = dict(scene, characters=[{"position": "Left", "storyCharacterId": anna["id"]}, {"position": "Left", "storyCharacterId": guard["id"]}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"два персонажі на одній позиції відхилено ({s})", s == 400)
bad = dict(scene, choices=[{"text": "x", "nextSceneId": str(uuid.uuid4()), "priceCrystals": 0}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"вибір у чужу сцену відхилено ({s})", s == 400)
bad = dict(scene, choices=[{"text": "x", "priceCrystals": 5}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
bad = dict(scene, choices=[{"text": "x", "priceCrystals": 31}])
s2, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"ціна вибору поза 10–30 відхилена ({s}, {s2})", s == 400 and s2 == 400)
bad = dict(scene, characters=[{"position": "Left", "storyCharacterId": str(uuid.uuid4())}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"невідомий персонаж у сцені відхилений ({s})", s == 400)
staged = dict(scene, characters=[
    {"position": "Left", "storyCharacterId": anna["id"], "emotion": "angry", "x": 40, "y": 10, "scale": 1.3, "flip": True},
    {"position": "Right", "storyCharacterId": guard["id"]}])
s, placed = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", staged, token=tok)
left = placed["characters"][0] if s == 200 else {}
ok(f"положення і власний стан у сцені ({s})",
   s == 200 and (left["x"], left["y"], left["scale"], left["flip"], left["emotion"], left["imagePath"]) == (40, 10, 1.3, True, "angry", anna_img)
   and placed["characters"][1]["x"] == 75)
s, framed_scene = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", dict(staged, backgroundFocusX=20, backgroundFocusY=80, backgroundZoom=1.6), token=tok)
s2, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", dict(staged, backgroundZoom=4), token=tok)
ok(f"кадр фону сцени збережено, масштаб поза межами відхилено ({s}, {s2})",
   s == 200 and (framed_scene["backgroundFocusX"], framed_scene["backgroundFocusY"], framed_scene["backgroundZoom"]) == (20, 80, 1.6) and s2 == 400)
s, overview = call("GET", "/studio/overview", token=tok)
ok(f"робочий стіл автора: остання історія і кількість реплік ({s})",
   s == 200 and overview["recentStories"][0]["story"]["id"] == sid and overview["recentStories"][0]["totalLines"] == 2 and overview["storyCount"] == 1)
bad = dict(scene, characters=[{"position": "Right", "storyCharacterId": guard["id"], "emotion": "angry"}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
bad = dict(scene, characters=[{"position": "Left", "storyCharacterId": anna["id"], "x": 150}])
s2, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"чужий стан і положення поза сценою відхилено ({s}, {s2})", s == 400 and s2 == 400)
angry_id = anna["states"][1]["id"]
neutral_id = anna["states"][0]["id"]
call("PUT", f"/stories/{sid}/characters/{anna['id']}",
     {"name": "Anna", "states": [{"id": neutral_id, "key": "neutral", "imagePath": anna_img}, {"id": angry_id, "key": "furious", "imagePath": anna_img}]}, token=tok)
s, after_rename = call("GET", f"/stories/{sid}/scenes/{sc1['id']}", token=tok)
call("PUT", f"/stories/{sid}/characters/{anna['id']}",
     {"name": "Anna", "states": [{"id": neutral_id, "key": "neutral", "imagePath": anna_img}]}, token=tok)
s2, after_delete = call("GET", f"/stories/{sid}/scenes/{sc1['id']}", token=tok)
ok("перейменування й видалення стану оновлюють сцену",
   after_rename["characters"][0]["emotion"] == "furious" and after_delete["characters"][0]["emotion"] == "neutral")
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", scene, token=tok)
s, renamed = call("PUT", f"/stories/{sid}/characters/{guard['id']}", {"name": "Officer"}, token=tok)
s2, full_story = call("GET", f"/stories/{sid}", token=tok)
first = [x for x in full_story["scenes"] if x["id"] == sc1["id"]][0]
ok(f"перейменування персонажа оновлює репліки й сцену ({s})", s == 200 and first["lines"][1]["speaker"] == "Officer" and first["characters"][1]["name"] == "Officer")
s, student = call("POST", "/auth/register", {"email": f"student{sfx}@test.local", "username": f"student_{sfx}", "password": "secret-pass-1", "displayName": "Student", "role": "Student"})
st = student["token"]
s, _ = call("POST", "/stories", {"title": "X", "level": "A1", "priceCrystals": 0}, token=st)
ok(f"учень не може створити історію ({s})", s == 403)
s, _ = call("POST", "/ml/analyze", {"texts": ["Hello there, my friend."], "level": "A2"}, token=st)
ok(f"учень не має доступу до ML-аналізу ({s})", s == 403)
s, live = call("POST", "/ml/analyze", {"texts": ["I purchased a magnificent gift for my grandmother.", "Yes."], "level": "A2"}, token=tok)
if s == 503:
    print("SKIP ML-сервіс не запущено — перевірки рівня пропущено")
else:
    purchased = next((w for w in live["results"][0]["words"] if w["text"] == "purchased"), {}) if s == 200 else {}
    ok(f"живий аналіз: рівень, складне слово і простіший відповідник ({s})",
       s == 200 and live["results"][0]["level"] in ["A1", "A2", "B1", "B2", "C"]
       and purchased.get("aboveTarget") and "buy" in purchased.get("simpler", []) and live["results"][1]["tooShort"])
    s, calibration = call("GET", "/ml/calibration", token=tok)
    ok(f"таблиця впевненості моделі: межі й червоний від 80% ({s})",
       s == 200 and calibration["redFrom"] == 0.8 and calibration["scales"]["dialogue"]["edges"][-1] == 2.5
       and len(calibration["scales"]["narration"]["bins"]) == 7)
    s, rated_scene = call("GET", f"/stories/{sid}/scenes/{sc1['id']}", token=tok)
    ok(f"репліки отримали рівень під час збереження ({s})", s == 200 and all(line["level"] for line in rated_scene["lines"]))
s, _ = call("POST", "/stories", {"title": "X", "level": "A1", "priceCrystals": 0})
ok(f"без входу — 401 ({s})", s == 401)
s, _ = call("GET", f"/stories/{sid}", token=st)
ok(f"чернетку не видно учню ({s})", s == 404)
s, pub = call("POST", f"/stories/{sid}/publish", token=tok)
ok(f"публікація ({s})", s == 200 and pub["status"] == "Published")
s, full = call("GET", f"/stories/{sid}", token=st)
ok(f"учень бачить опубліковану історію ({s})", s == 200 and full["startSceneId"] == sc1["id"] and len(full["scenes"]) == 2)
s, lst = call("GET", "/stories?level=A2")
ok(f"список з фільтром за рівнем ({s})", s == 200 and any(x["id"] == sid for x in lst))
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", scene, token=st)
ok(f"учень не може редагувати сцену ({s})", s == 403)
s, _ = call("DELETE", f"/stories/{sid}/scenes/{sc2['id']}", token=tok)
s2, after = call("GET", f"/stories/{sid}", token=tok)
ok(f"видалення сцени обнуляє вибір, що вів до неї ({s})", s == 204 and after["scenes"][0]["choices"][0]["nextSceneId"] is None)
free_choice, paid_choice = after["scenes"][0]["choices"][0]["id"], after["scenes"][0]["choices"][1]["id"]


def sql(statement):
    subprocess.run(["docker", "compose", "exec", "-T", "postgres", "sh", "-c",
                    'psql -q -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "$0"', statement], check=True, capture_output=True)


def play(token, choice=None):
    _, session = call("POST", f"/play/{sid}/start", token=token)
    spent = None
    if choice:
        _, result = call("POST", f"/play/sessions/{session['sessionId']}/choice", {"choiceId": choice}, token=token)
        spent = result
    _, summary = call("POST", f"/play/sessions/{session['sessionId']}/finish", token=token)
    return session, spent, summary


s, wallet = call("GET", "/wallet", token=st)
s2, history = call("GET", "/wallet/transactions", token=st)
ok(f"новий учень має вітальні 50 кристалів і нульову серію ({s}, {s2})",
   s == 200 and wallet["balance"] == 50 and wallet["streak"]["current"] == 0 and history[0]["reason"] == "WelcomeBonus")
s, access = call("GET", f"/play/{sid}/access", token=st)
ok(f"безкоштовна історія доступна, нагорода за A2 — 12 ({s})", s == 200 and access["owned"] and access["reward"] == 12 and not access["free"])
session, choice_result, summary = play(st, paid_choice)
ok(f"платний вибір списує 10 кристалів ({choice_result})", choice_result["spent"] == 10 and choice_result["balance"] == 40)
ok(f"перше проходження: +12 за історію, +5 щоденна ціль, серія 1 ({summary['rewards']})",
   summary["completed"] and sorted(r["amount"] for r in summary["rewards"]) == [5, 12] and summary["balance"] == 57 and summary["streak"]["current"] == 1)
s, wallet = call("GET", "/wallet", token=st)
today_cell = [d for d in wallet["week"] if d["isToday"]]
ok(f"тиждень серії: 7 днів, сьогодні виконано ({s})", len(wallet["week"]) == 7 and len(today_cell) == 1 and today_cell[0]["state"] == "Done")
s, recent = call("GET", "/play/recent", token=st)
ok(f"нещодавні історії учня ({s})", s == 200 and recent[0]["story"]["id"] == sid and recent[0]["completed"])
session, choice_result, summary = play(st, paid_choice)
ok(f"куплений вибір не списується вдруге, повтор історії без нагороди ({choice_result['spent']}, {summary['rewards']})",
   paid_choice in session["purchasedChoiceIds"] and choice_result["spent"] == 0 and summary["rewards"] == [] and summary["balance"] == 57)
_, _, summary = play(st)
ok(f"незавершене проходження не дає нагороди ({summary['completed']})", not summary["completed"] and summary["rewards"] == [])
s, wallet_a = call("GET", "/wallet", token=tok)
_, choice_result, summary = play(tok, paid_choice)
s2, wallet_a2 = call("GET", "/wallet", token=tok)
ok("автор у своїй історії нічого не платить і не заробляє",
   summary["free"] and choice_result["spent"] == 0 and summary["rewards"] == [] and wallet_a["balance"] == wallet_a2["balance"])
call("PUT", f"/stories/{sid}", {"title": "Lost at the Station", "level": "A2", "priceCrystals": 100, "tagIds": [1, 8], "words": story["words"]}, token=tok)
s, access = call("GET", f"/play/{sid}/access", token=st)
s2, _ = call("POST", f"/play/{sid}/start", token=st)
s3, _ = call("POST", f"/play/{sid}/purchase", token=st)
ok(f"платну історію не можна почати без покупки, на 100 не вистачає ({s2}, {s3})", not access["owned"] and s2 == 402 and s3 == 409)
call("PUT", f"/stories/{sid}", {"title": "Lost at the Station", "level": "A2", "priceCrystals": 50, "tagIds": [1, 8], "words": story["words"]}, token=tok)
s, bought = call("POST", f"/play/{sid}/purchase", token=st)
s2, bought_again = call("POST", f"/play/{sid}/purchase", token=st)
s3, _ = call("POST", f"/play/{sid}/start", token=st)
ok(f"купівля за 50 один раз, далі історія доступна ({s}, {s2}, {s3})",
   s == 200 and bought["owned"] and bought["balance"] == 7 and bought_again["balance"] == 7 and s3 == 200)
s, _ = call("POST", "/wallet/streak/rescue", token=st)
ok(f"відновлювати нічого, коли серія не обірвана ({s})", s == 409)
student_id = student["user"]["id"]
sql(f"""UPDATE "Streaks" SET "CurrentDays" = 6, "LastActiveDate" = (now() at time zone 'Europe/Kyiv')::date - 2 WHERE "UserId" = '{student_id}'""")
s, wallet = call("GET", "/wallet", token=st)
s2, _ = call("POST", "/wallet/streak/rescue", token=st)
ok(f"пропущений день: серія 0, можна відновити 6 днів, але 7 кристалів замало ({s2})",
   wallet["streak"]["current"] == 0 and wallet["streak"]["rescueDays"] == 6 and s2 == 409)
sql(f"""UPDATE "Users" SET "CrystalBalance" = 100 WHERE "Id" = '{student_id}'""")
s, rescued = call("POST", "/wallet/streak/rescue", token=st)
ok(f"відновлення за 20 повертає 6 днів ({s})",
   s == 200 and rescued["wallet"]["streak"]["current"] == 6 and not rescued["wallet"]["streak"]["playedToday"] and rescued["wallet"]["balance"] == 80)
_, _, summary = play(st, free_choice)
ok(f"проходження сьогодні: серія 7, щоденна ціль +5 і бонус за 7 днів +30 ({summary['rewards']})",
   summary["streak"]["current"] == 7 and sorted(r["amount"] for r in summary["rewards"]) == [5, 30] and summary["balance"] == 115)
sql(f"""UPDATE "Streaks" SET "CurrentDays" = 4, "LastActiveDate" = (now() at time zone 'Europe/Kyiv')::date - 3, "RescuableDays" = 0, "RescuableUntil" = NULL WHERE "UserId" = '{student_id}'""")
s, wallet = call("GET", "/wallet", token=st)
ok(f"два пропущені дні: серія обнуляється без відновлення ({s})", wallet["streak"]["current"] == 0 and wallet["streak"]["rescueDays"] == 0)
call("PUT", f"/stories/{sid}", {"title": "Lost at the Station", "level": "A2", "priceCrystals": 0, "tagIds": [1, 8], "words": story["words"]}, token=tok)
s, _ = call("POST", "/auth/register", {"email": f"t{sfx}@test.local", "username": f"t_{sfx}", "password": "secret-pass-1", "displayName": "Teacher", "role": "Teacher"})
ok(f"роль Teacher під час реєстрації не існує ({s})", s == 400)
s, _ = call("POST", "/auth/register", {"email": f"other{sfx}@test.local", "username": f"student_{sfx}", "password": "secret-pass-1", "displayName": "Other", "role": "Student"})
ok(f"зайнятий нік відхилено ({s})", s == 409)
s, inv = call("POST", "/teaching/invitations", {"emailOrUsername": f"@student_{sfx}"}, token=tok)
ok(f"автор запрошує учня за ніком ({s})", s == 200 and inv["status"] == "Pending")
s, _ = call("POST", "/teaching/invitations", {"emailOrUsername": f"student{sfx}@test.local"}, token=tok)
ok(f"повторне запрошення відхилено ({s})", s == 409)
s, _ = call("POST", "/teaching/invitations", {"emailOrUsername": "nobody_here_404"}, token=tok)
ok(f"неіснуючий учень ({s})", s == 404)
s, _ = call("POST", "/teaching/invitations", {"emailOrUsername": f"author_{sfx}"}, token=tok)
ok(f"автора не можна запросити як учня ({s})", s == 404)
s, _ = call("POST", "/teaching/invitations", {"emailOrUsername": f"author_{sfx}"}, token=st)
ok(f"учень не може запрошувати ({s})", s == 403)
s, me_a = call("GET", "/auth/me", token=tok)
ok(f"до прийняття бейджа Teacher немає ({s})", s == 200 and me_a["isTeacher"] is False)
s, pending = call("GET", "/teaching/teachers", token=st)
ok(f"учень бачить запрошення у профілі ({s})", s == 200 and len(pending) == 1 and pending[0]["status"] == "Pending")
s, _ = call("POST", f"/teaching/links/{inv['id']}/accept", token=tok)
ok(f"автор не може прийняти за учня ({s})", s == 404)
s, acc = call("POST", f"/teaching/links/{inv['id']}/accept", token=st)
ok(f"учень приймає запрошення ({s})", s == 200 and acc["status"] == "Accepted" and acc["teacher"]["isTeacher"] is True)
s, me_a = call("GET", "/auth/me", token=tok)
ok(f"після прийняття автор має бейдж Teacher ({s})", me_a["isTeacher"] is True)
s, studs = call("GET", "/teaching/students", token=tok)
ok(f"автор бачить своїх учнів ({s})", s == 200 and len(studs) == 1)
s, _ = call("DELETE", f"/teaching/links/{inv['id']}", token=st)
s2, me_a = call("GET", "/auth/me", token=tok)
ok(f"учень розриває зв'язок, бейдж зникає ({s})", s == 204 and me_a["isTeacher"] is False)
s, lst = call("GET", "/stories?level=A2")
mine = [x for x in lst if x["id"] == sid][0]
ok(f"у списку історій нік автора є, ім'я приховане ({s})", mine["authorUsername"] == f"author_{sfx}" and mine["authorDisplayName"] is None)
s, inv2 = call("POST", "/teaching/invitations", {"emailOrUsername": f"student_{sfx}"}, token=tok)
s, studs = call("GET", "/teaching/students", token=tok)
ok(f"до прийняття ім'я учня приховане ({s})", studs[0]["student"]["displayName"] is None)
call("POST", f"/teaching/links/{inv2['id']}/accept", token=st)
s, studs = call("GET", "/teaching/students", token=tok)
ok(f"ім'я Private не видно навіть викладачу ({s})", studs[0]["student"]["displayName"] is None)
s, prof = call("PATCH", "/profile", {"nameVisibility": "Teachers"}, token=st)
ok(f"учень відкриває ім'я для викладачів ({s})", s == 200 and prof["nameVisibility"] == "Teachers")
s, studs = call("GET", "/teaching/students", token=tok)
ok(f"викладач бачить ім'я учня ({s})", studs[0]["student"]["displayName"] == "Student")
s, other = call("POST", "/auth/register", {"email": f"other{sfx}@test.local", "username": f"other_{sfx}", "password": "secret-pass-1", "displayName": "Other Author", "role": "Author"})
s, prof_view = call("GET", f"/authors/author_{sfx}", token=other["token"])
ok(f"сторонній не бачить імені автора ({s})", s == 200 and prof_view["author"]["displayName"] is None and prof_view["author"]["isTeacher"] is True)
call("PATCH", "/profile", {"nameVisibility": "Public"}, token=tok)
s, prof_view = call("GET", f"/authors/author_{sfx}", token=other["token"])
ok(f"публічне ім'я автора видно всім ({s})", prof_view["author"]["displayName"] == "Test Author")
s, _ = call("POST", f"/authors/author_{sfx}/follow", token=st)
s2, _ = call("POST", f"/authors/author_{sfx}/follow", token=st)
ok(f"підписка на автора, повторна не дублюється ({s}, {s2})", s == 204 and s2 == 204)
s, prof_view = call("GET", f"/authors/@author_{sfx}", token=st)
ok(f"профіль автора: 1 підписник, я підписаний ({s})", prof_view["followerCount"] == 1 and prof_view["isFollowedByMe"] is True)
s, feed = call("GET", "/stories/following", token=st)
ok(f"стрічка підписок містить історію автора ({s})", s == 200 and any(x["id"] == sid for x in feed))
s, fl = call("GET", "/follows", token=st)
ok(f"список підписок ({s})", s == 200 and fl[0]["username"] == f"author_{sfx}")
s, _ = call("POST", f"/authors/student_{sfx}/follow", token=tok)
ok(f"на учня підписатися не можна ({s})", s == 404)
s, _ = call("DELETE", f"/authors/author_{sfx}/follow", token=st)
s2, feed = call("GET", "/stories/following", token=st)
ok(f"відписка ({s})", s == 204 and len(feed) == 0)
def latest_code(addr):
    with urllib.request.urlopen("http://localhost:8025/api/v1/search?query=" + urllib.parse.quote("to:" + addr)) as r:
        msgs = json.loads(r.read())["messages"]
    if not msgs:
        return None
    with urllib.request.urlopen("http://localhost:8025/api/v1/message/" + msgs[0]["ID"]) as r:
        text = json.loads(r.read())["Text"]
    return re.search(r"\b(\d{6})\b", text).group(1)
email_s = f"student{sfx}@test.local"
s, r = call("POST", "/auth/password-reset/request", {"email": email_s})
s2, r2 = call("POST", "/auth/password-reset/request", {"email": f"nobody{sfx}@test.local"})
ok(f"однакова відповідь для існуючої і неіснуючої пошти ({s}, {s2})", s == s2 == 202 and r == r2)
time.sleep(1.5)
code = latest_code(email_s)
ok(f"лист з 6-значним кодом прийшов у Mailpit", code is not None)
wrong = "000000" if code != "000000" else "111111"
s, _ = call("POST", "/auth/password-reset/confirm", {"email": email_s, "code": wrong, "newPassword": "new-pass-123"})
ok(f"неправильний код відхилено ({s})", s == 400)
s, _ = call("POST", "/auth/password-reset/confirm", {"email": email_s, "code": code, "newPassword": "new-pass-123"})
ok(f"правильний код змінює пароль ({s})", s == 204)
s, _ = call("POST", "/auth/password-reset/confirm", {"email": email_s, "code": code, "newPassword": "again-pass-123"})
ok(f"код одноразовий ({s})", s == 400)
s, _ = call("POST", "/auth/login", {"email": email_s, "password": "secret-pass-1"})
s2, _ = call("POST", "/auth/login", {"email": email_s, "password": "new-pass-123"})
ok(f"старий пароль не працює, новий працює ({s}, {s2})", s == 401 and s2 == 200)
s, _ = call("POST", "/auth/password-reset/request", {"email": email_s})
time.sleep(1.0)
ok(f"новий код не надсилається частіше ніж раз на хвилину", latest_code(email_s) == code)
s, me_avatar = call("POST", "/profile/avatar", raw=form({}, "me.png", png()), token=st, ctype=mp)
avatar = me_avatar["avatar"]["path"] if s == 200 else ""
ok(f"фото профілю завантажено ({s})", s == 200 and avatar.startswith("/uploads/avatars/")
   and urllib.request.urlopen("http://localhost:5080" + avatar).status == 200)
s, framed_avatar = call("PUT", "/profile/avatar/framing", {"focusX": 30, "focusY": 60, "zoom": 1.5}, token=st)
s2, _ = call("PUT", "/profile/avatar/framing", {"focusX": 30, "focusY": 60, "zoom": 9}, token=st)
ok(f"кадр фото профілю збережено, масштаб поза межами відхилено ({s}, {s2})",
   s == 200 and (framed_avatar["avatar"]["focusX"], framed_avatar["avatar"]["zoom"]) == (30, 1.5) and s2 == 400)
s, _ = call("POST", "/profile/password", {"currentPassword": "wrong-pass-0", "newPassword": "changed-pass-1"}, token=st)
s2, _ = call("POST", "/profile/password", {"currentPassword": "new-pass-123", "newPassword": "changed-pass-1"}, token=st)
s3, _ = call("POST", "/auth/login", {"email": email_s, "password": "changed-pass-1"})
ok(f"зміна пароля: неправильний поточний відхилено, правильний змінює ({s}, {s2}, {s3})", s == 400 and s2 == 204 and s3 == 200)
new_email = f"moved{sfx}@test.local"
s, _ = call("POST", "/profile/email", {"newEmail": f"author{sfx}@test.local", "password": "changed-pass-1"}, token=st)
s2, _ = call("POST", "/profile/email", {"newEmail": new_email, "password": "wrong-pass-0"}, token=st)
ok(f"нову пошту не можна взяти чужу або без пароля ({s}, {s2})", s == 400 and s2 == 400)
s, _ = call("POST", "/profile/email", {"newEmail": new_email, "password": "changed-pass-1"}, token=st)
time.sleep(1.5)
email_code = latest_code(new_email)
s2, _ = call("POST", "/profile/email/confirm", {"code": "000000" if email_code != "000000" else "111111"}, token=st)
s3, moved = call("POST", "/profile/email/confirm", {"code": email_code}, token=st)
s4, _ = call("POST", "/auth/login", {"email": new_email, "password": "changed-pass-1"})
ok(f"зміна пошти через код на нову адресу ({s}, {s2}, {s3}, {s4})",
   s == 202 and email_code is not None and s2 == 400 and s3 == 200 and moved["email"] == new_email and s4 == 200)
s, _ = call("DELETE", f"/stories/{sid}", token=tok)
ok(f"видалення історії ({s})", s == 204)
s, _ = call("DELETE", "/profile", {"password": "wrong-password"}, token=tok)
ok(f"видалення акаунта з неправильним паролем відхилено ({s})", s == 400)
s, _ = call("DELETE", "/profile", {"password": "secret-pass-1"}, token=tok)
s2, _ = call("POST", "/auth/login", {"email": f"author{sfx}@test.local", "password": "secret-pass-1"})
ok(f"автор видалив акаунт, увійти вже не можна ({s}, {s2})", s == 204 and s2 == 401)
try:
    gone = urllib.request.urlopen("http://localhost:5080" + bg["imagePath"]).status
except urllib.error.HTTPError as e:
    gone = e.code
ok(f"фон автора видалено разом з акаунтом ({gone})", gone == 404)
s, again = call("POST", "/tags", {"name": f"Smoke tag {sfx}", "kind": "Topic"}, token=other["token"])
ok(f"власні теги видаленого автора прибрано ({s})", s == 200 and again["id"] != own["id"])
s, _ = call("DELETE", "/profile", {"password": "changed-pass-1"}, token=st)
try:
    avatar_gone = urllib.request.urlopen("http://localhost:5080" + avatar).status
except urllib.error.HTTPError as e:
    avatar_gone = e.code
s2, _ = call("DELETE", "/profile", {"password": "secret-pass-1"}, token=other["token"])
ok(f"тестові учень і автор видалені ({s}, {s2})", s == 204 and s2 == 204)
ok(f"фото профілю видалено разом з акаунтом ({avatar_gone})", avatar_gone == 404)
print(f"\nПеревірок: {checks}, помилок: {len(failures)}")
sys.exit(1 if failures else 0)
