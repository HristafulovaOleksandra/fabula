import json
import re
import sys
import time
import urllib.parse
import struct
import urllib.request
import uuid
import zlib

B = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5080/api"
failures = []
checks = 0


def call(method, path, body=None, token=None, raw=None, ctype="application/json"):
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
s, me = call("GET", "/auth/me", token=tok)
ok(f"/auth/me без хешу пароля ({s})", s == 200 and "passwordHash" not in json.dumps(me))
s, tags = call("GET", "/tags")
ok(f"теги ({s}, {len(tags)})", s == 200 and len(tags) == 19)
s, story = call("POST", "/stories", {"title": "Lost at the Station", "description": "Test", "level": "A2", "priceCrystals": 0,
                                     "tagIds": [1, 8], "words": [{"word": "ticket", "translation": "квиток"}, {"word": "platform", "translation": "платформа"}]}, token=tok)
sid = story["id"]
ok(f"створення історії ({s})", s == 201 and story["status"] == "Draft" and len(story["words"]) == 2)
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
scene = {"title": "Arrival", "backgroundId": bg["id"],
         "characters": [{"position": "Left", "name": "Anna", "emotion": "happy"}, {"position": "Right", "name": "Guard"}],
         "lines": [{"speaker": "Anna", "text": "Excuse me, where can I buy a ticket?"},
                   {"speaker": "Guard", "text": "The ticket office is next to platform two."}],
         "choices": [{"text": "Go to the ticket office", "nextSceneId": sc2["id"], "priceCrystals": 0},
                     {"text": "Ask again", "priceCrystals": 10}]}
s, saved = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", scene, token=tok)
ok(f"збереження сцени одним запитом ({s})", s == 200 and len(saved["lines"]) == 2 and len(saved["choices"]) == 2 and saved["backgroundImagePath"])
bad = dict(scene, characters=[{"position": "Left", "name": "A"}, {"position": "Left", "name": "B"}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"два персонажі на одній позиції відхилено ({s})", s == 400)
bad = dict(scene, choices=[{"text": "x", "nextSceneId": str(uuid.uuid4()), "priceCrystals": 0}])
s, _ = call("PUT", f"/stories/{sid}/scenes/{sc1['id']}", bad, token=tok)
ok(f"вибір у чужу сцену відхилено ({s})", s == 400)
s, student = call("POST", "/auth/register", {"email": f"student{sfx}@test.local", "username": f"student_{sfx}", "password": "secret-pass-1", "displayName": "Student", "role": "Student"})
st = student["token"]
s, _ = call("POST", "/stories", {"title": "X", "level": "A1", "priceCrystals": 0}, token=st)
ok(f"учень не може створити історію ({s})", s == 403)
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
s, _ = call("DELETE", "/profile", {"password": "new-pass-123"}, token=st)
s2, _ = call("DELETE", "/profile", {"password": "secret-pass-1"}, token=other["token"])
ok(f"тестові учень і автор видалені ({s}, {s2})", s == 204 and s2 == 204)
print(f"\nПеревірок: {checks}, помилок: {len(failures)}")
sys.exit(1 if failures else 0)
