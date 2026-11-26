#!/usr/bin/env python3
"""game.html(본문)을 감싸서 홈 화면 설치가 가능한 독립 실행 index.html을 만든다.

game.html은 Claude 아티팩트(미리보기 링크)용 본문이고,
index.html은 GitHub Pages 같은 곳에 올려서 '홈 화면에 추가'로 앱처럼 쓰는 용도다.
사용법: python3 web/build.py
"""
import pathlib

HERE = pathlib.Path(__file__).parent
body = (HERE / "game.html").read_text(encoding="utf-8")

head = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover">
<link rel="manifest" href="manifest.webmanifest">
<link rel="apple-touch-icon" href="icon-180.png">
<link rel="icon" type="image/png" href="icon-192.png">
<style>
:root { padding-top: env(safe-area-inset-top, 0px); padding-bottom: env(safe-area-inset-bottom, 0px); box-sizing: border-box; height: 100%; }
*, *::before, *::after { box-sizing: border-box; }
body { margin: 0; font: 14px system-ui, -apple-system, sans-serif; }
[hidden] { display: none !important; }
</style>
"""
out = head + body.replace("<title>", "<title>", 1)
# 본문 앞부분의 <title>/<meta>/<link>/<style>도 head 안에 두고, 마크업부터 body로 연다.
split = out.index('<div id="app">')
out = out[:split] + "</head>\n<body>\n" + out[split:] + "\n</body>\n</html>\n"
(HERE / "index.html").write_text(out, encoding="utf-8")
print("wrote", HERE / "index.html")
