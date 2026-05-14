"""
Generate a SonarCloud SAST HTML report for UniRide.

Usage:
    python generate_sonar_report.py --token <SONAR_TOKEN> [--output <path>]

The token can also be set via the SONAR_TOKEN environment variable.
"""

import argparse
import json
import os
import sys
import urllib.request
import urllib.parse
from datetime import datetime, timezone

PROJECT_KEY = "shayaanhu_devsecops-secure-app-pipeline"
ORGANIZATION = "shayaanhu"
BASE_URL = "https://sonarcloud.io"


# ── API helpers ────────────────────────────────────────────────────────────────

def api_get(path, params, token):
    url = f"{BASE_URL}{path}?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url)
    req.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read())


def fetch_quality_gate(token):
    data = api_get("/api/qualitygates/project_status",
                   {"projectKey": PROJECT_KEY}, token)
    return data["projectStatus"]


def fetch_metrics(token):
    metrics = [
        "ncloc", "coverage", "duplicated_lines_density",
        "reliability_rating", "security_rating", "sqale_rating",
        "vulnerabilities", "bugs", "code_smells", "security_hotspots",
    ]
    data = api_get("/api/measures/component",
                   {"component": PROJECT_KEY, "metricKeys": ",".join(metrics)},
                   token)
    return {m["metric"]: m.get("value", "—") for m in data["component"]["measures"]}


def fetch_issues(issue_type, token):
    results = []
    page = 1
    while True:
        data = api_get("/api/issues/search", {
            "componentKeys": PROJECT_KEY,
            "types": issue_type,
            "ps": 500,
            "p": page,
        }, token)
        results.extend(data["issues"])
        if len(results) >= data["paging"]["total"]:
            break
        page += 1
    return results


def fetch_hotspots(token):
    results = []
    page = 1
    while True:
        data = api_get("/api/hotspots/search", {
            "projectKey": PROJECT_KEY,
            "ps": 500,
            "p": page,
        }, token)
        results.extend(data["hotspots"])
        if len(results) >= data["paging"]["total"]:
            break
        page += 1
    return results


# ── Rating helpers ─────────────────────────────────────────────────────────────

def rating_letter(val):
    return {1: "A", 2: "B", 3: "C", 4: "D", 5: "E"}.get(int(float(val or 1)), "?")


def rating_color(val):
    return {1: "#27ae60", 2: "#8bc34a", 3: "#f0a500", 4: "#e67e22", 5: "#e74c3c"}.get(
        int(float(val or 1)), "#999"
    )


SEVERITY_COLORS = {
    "BLOCKER":  "#7b0000",
    "CRITICAL": "#cc2200",
    "MAJOR":    "#e67e22",
    "MINOR":    "#2563eb",
    "INFO":     "#888888",
}

PRIORITY_COLORS = {
    "HIGH":   "#cc2200",
    "MEDIUM": "#e67e22",
    "LOW":    "#2563eb",
}


def badge(text, color):
    return f'<span class="badge" style="background:{color}">{text}</span>'


def issue_location(issue):
    comp = issue.get("component", "").replace(f"{PROJECT_KEY}:", "")
    line = issue.get("line") or issue.get("textRange", {}).get("startLine", "")
    loc = f'<span class="mono">{comp}'
    if line:
        loc += f'<br><span style="color:#888;font-size:10px">line {line}</span>'
    loc += "</span>"
    return loc


def hotspot_location(hs):
    comp = hs.get("component", "").replace(f"{PROJECT_KEY}:", "")
    line = hs.get("line") or hs.get("textRange", {}).get("startLine", "")
    loc = f'<span class="mono">{comp}'
    if line:
        loc += f'<br><span style="color:#888;font-size:10px">line {line}</span>'
    loc += "</span>"
    return loc


# ── HTML builder ───────────────────────────────────────────────────────────────

CSS = """
* { box-sizing: border-box; margin: 0; padding: 0; }
body { font-family: Arial, sans-serif; background: #f4f6f9; color: #222; }
.header { background: #1440a8; color: #f5ead6; padding: 28px 48px; }
.header h1 { font-size: 24px; font-weight: 700; }
.header p  { margin-top: 6px; opacity: .75; font-size: 13px; }
.qg-banner { padding: 14px 48px; font-size: 15px; font-weight: 700;
             color: #fff; }
.summary { display: flex; gap: 16px; padding: 20px 48px; background: #fff;
            border-bottom: 1px solid #dde; flex-wrap: wrap; align-items: center; }
.stat { text-align: center; padding: 14px 24px; border-radius: 8px; min-width: 100px; }
.stat .n { font-size: 30px; font-weight: 700; }
.stat .l { font-size: 11px; margin-top: 4px; opacity: .7; }
.metrics { display: flex; gap: 16px; padding: 16px 48px; background: #fff;
            border-bottom: 1px solid #dde; flex-wrap: wrap; }
.metric { padding: 12px 20px; border-radius: 8px; background: #f4f6f9;
           text-align: center; min-width: 120px; }
.metric .mv { font-size: 22px; font-weight: 700; color: #1440a8; }
.metric .ml { font-size: 11px; color: #666; margin-top: 3px; }
.wrap { padding: 24px 48px; }
.section-title { font-size: 16px; font-weight: 700; color: #1440a8;
                  margin: 28px 0 12px; border-left: 4px solid #1440a8;
                  padding-left: 10px; }
table { width: 100%; border-collapse: collapse; background: #fff;
         box-shadow: 0 1px 4px rgba(0,0,0,.08); border-radius: 8px;
         overflow: hidden; margin-bottom: 8px; }
th { background: #1440a8; color: #f5ead6; padding: 10px 14px;
      text-align: left; font-size: 11px; text-transform: uppercase; letter-spacing: .06em; }
td { padding: 10px 14px; border-bottom: 1px solid #eee; font-size: 12px; vertical-align: top; }
tr:last-child td { border-bottom: none; }
tr:hover td { filter: brightness(0.97); }
.badge { display: inline-block; padding: 2px 10px; border-radius: 4px;
          color: #fff; font-size: 11px; font-weight: 700; white-space: nowrap; }
.mono { font-family: monospace; font-size: 11px; word-break: break-all; }
.empty { padding: 40px; text-align: center; color: #27ae60; font-size: 15px;
          background: #fff; border-radius: 8px;
          box-shadow: 0 1px 4px rgba(0,0,0,.08); margin-bottom: 8px; }
.toc { background: #fff; border-radius: 8px; padding: 20px 28px;
        box-shadow: 0 1px 4px rgba(0,0,0,.08); margin-bottom: 24px; }
.toc a { color: #1440a8; text-decoration: none; font-size: 13px; }
.toc a:hover { text-decoration: underline; }
.toc li { margin: 6px 0; }
"""


def issues_table(issues, cols):
    if not issues:
        return '<div class="empty">&#10003; No issues found</div>'
    rows = ""
    for i in issues:
        sev = i.get("severity", "INFO")
        color = SEVERITY_COLORS.get(sev, "#888")
        bg = {"BLOCKER": "#fff0ee", "CRITICAL": "#fff0ee",
              "MAJOR": "#fff8ee", "MINOR": "#eef3ff", "INFO": "#f8f8f8"}.get(sev, "")
        cells = ""
        for col in cols:
            if col == "severity":
                cells += f"<td>{badge(sev, color)}</td>"
            elif col == "rule":
                cells += f'<td class="mono">{i.get("rule","")}</td>'
            elif col == "location":
                cells += f"<td>{issue_location(i)}</td>"
            elif col == "message":
                cells += f"<td>{i.get('message','')}</td>"
        rows += f'<tr style="background:{bg}">{cells}</tr>'
    headers = {"severity": "Severity", "rule": "Rule",
               "location": "Location", "message": "Message"}
    ths = "".join(f"<th>{headers[c]}</th>" for c in cols)
    return f"<table><thead><tr>{ths}</tr></thead><tbody>{rows}</tbody></table>"


def hotspots_table(hotspots):
    if not hotspots:
        return '<div class="empty">&#10003; No hotspots found</div>'
    rows = ""
    for hs in hotspots:
        pri = hs.get("vulnerabilityProbability", "MEDIUM")
        color = PRIORITY_COLORS.get(pri, "#888")
        cat = hs.get("securityCategory", "")
        msg = hs.get("message", "")
        rows += (f"<tr><td>{badge(pri, color)}</td>"
                 f'<td class="mono">{cat}</td>'
                 f"<td>{hotspot_location(hs)}</td>"
                 f"<td>{msg}</td></tr>")
    return (f"<table><thead><tr><th>Priority</th><th>Category</th>"
            f"<th>Location</th><th>Message</th></tr></thead><tbody>{rows}</tbody></table>")


def qg_table(qg):
    rows = ""
    for cond in qg.get("conditions", []):
        status = cond.get("status", "")
        color = "#27ae60" if status == "OK" else "#e74c3c"
        label = "PASS" if status == "OK" else "FAIL"
        metric = cond.get("metricKey", "")
        actual = cond.get("actualValue", "—")
        op = cond.get("comparator", "")
        threshold = cond.get("errorThreshold", "—")
        rows += (f"<tr><td>{badge(label, color)}</td>"
                 f'<td class="mono">{metric}</td>'
                 f'<td class="mono">{actual}</td>'
                 f'<td class="mono">{op} {threshold}</td></tr>')
    return (f"<table><thead><tr><th>Status</th><th>Metric</th>"
            f"<th>Actual</th><th>Threshold</th></tr></thead><tbody>{rows}</tbody></table>")


def build_html(qg, metrics, vulns, bugs, hotspots, smells):
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    qg_status = qg.get("status", "ERROR")
    qg_color = "#27ae60" if qg_status == "OK" else "#e74c3c"
    qg_label = "PASSED" if qg_status == "OK" else "FAILED"

    rel_r = rating_letter(metrics.get("reliability_rating", 1))
    sec_r = rating_letter(metrics.get("security_rating", 1))
    mnt_r = rating_letter(metrics.get("sqale_rating", 1))
    rel_c = rating_color(metrics.get("reliability_rating", 1))
    sec_c = rating_color(metrics.get("security_rating", 1))
    mnt_c = rating_color(metrics.get("sqale_rating", 1))

    n_vulns   = len(vulns)
    n_bugs    = len(bugs)
    n_spots   = len(hotspots)
    n_smells  = len(smells)
    critical  = sum(1 for i in vulns + bugs + smells if i.get("severity") in ("BLOCKER", "CRITICAL"))
    major     = sum(1 for i in vulns + bugs + smells if i.get("severity") == "MAJOR")
    minor     = sum(1 for i in vulns + bugs + smells if i.get("severity") in ("MINOR", "INFO"))
    total     = critical + major + minor + n_spots

    issue_cols = ["severity", "rule", "location", "message"]

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>SonarCloud SAST Report — UniRide</title>
<style>{CSS}</style>
</head>
<body>

<div class="header">
  <h1>SonarCloud SAST Report</h1>
  <p>UniRide &nbsp;·&nbsp; Project: {PROJECT_KEY} &nbsp;·&nbsp; Generated {now}</p>
</div>

<div class="qg-banner" style="background:{qg_color}">Quality Gate: {qg_label}</div>

<div class="summary">
  <div class="stat" style="background:#fff0ee">
    <div class="n" style="color:#cc2200">{critical}</div>
    <div class="l">Critical/Blocker</div>
  </div>
  <div class="stat" style="background:#fff8ee">
    <div class="n" style="color:#e67e22">{major}</div>
    <div class="l">Major</div>
  </div>
  <div class="stat" style="background:#eef3ff">
    <div class="n" style="color:#2563eb">{minor}</div>
    <div class="l">Minor/Info</div>
  </div>
  <div class="stat" style="background:#fff0ee">
    <div class="n" style="color:#cc2200">{n_vulns}</div>
    <div class="l">Vulnerabilities</div>
  </div>
  <div class="stat" style="background:#fff8ee">
    <div class="n" style="color:#e67e22">{n_bugs}</div>
    <div class="l">Bugs</div>
  </div>
  <div class="stat" style="background:#f0f0f0">
    <div class="n" style="color:#333">{n_spots}</div>
    <div class="l">Hotspots</div>
  </div>
  <div class="stat" style="background:#f0f0f0">
    <div class="n" style="color:#333">{total}</div>
    <div class="l">Total Issues</div>
  </div>
</div>

<div class="metrics">
  <div class="metric">
    <div class="mv">{metrics.get("ncloc", "—")}</div>
    <div class="ml">Lines of Code</div>
  </div>
  <div class="metric">
    <div class="mv">{metrics.get("coverage", "0")}%</div>
    <div class="ml">Coverage</div>
  </div>
  <div class="metric">
    <div class="mv">{metrics.get("duplicated_lines_density", "—")}%</div>
    <div class="ml">Duplication</div>
  </div>
  <div class="metric">
    <div class="mv">{badge(rel_r, rel_c)}</div>
    <div class="ml">Reliability</div>
  </div>
  <div class="metric">
    <div class="mv">{badge(sec_r, sec_c)}</div>
    <div class="ml">Security</div>
  </div>
  <div class="metric">
    <div class="mv">{badge(mnt_r, mnt_c)}</div>
    <div class="ml">Maintainability</div>
  </div>
</div>

<div class="wrap">

<div class="toc">
  <strong>Contents</strong>
  <ul style="margin-top:10px;padding-left:20px">
    <li><a href="#qg">Quality Gate Conditions</a></li>
    <li><a href="#vulns">Vulnerabilities ({n_vulns})</a></li>
    <li><a href="#bugs">Bugs ({n_bugs})</a></li>
    <li><a href="#hotspots">Security Hotspots ({n_spots})</a></li>
    <li><a href="#smells">Code Smells ({n_smells})</a></li>
  </ul>
</div>

<div id="qg" class="section-title">Quality Gate Conditions</div>
{qg_table(qg)}

<div id="vulns" class="section-title">Vulnerabilities ({n_vulns})</div>
{issues_table(vulns, issue_cols)}

<div id="bugs" class="section-title">Bugs ({n_bugs})</div>
{issues_table(bugs, issue_cols)}

<div id="hotspots" class="section-title">Security Hotspots ({n_spots})</div>
{hotspots_table(hotspots)}

<div id="smells" class="section-title">Code Smells ({n_smells})</div>
{issues_table(smells, issue_cols)}

</div>
</body>
</html>"""
    return html


# ── Entry point ────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Generate SonarCloud SAST HTML report")
    parser.add_argument("--token", default=os.environ.get("SONAR_TOKEN"),
                        help="SonarCloud token (or set SONAR_TOKEN env var)")
    parser.add_argument("--output", default="sonarcloud-report.html",
                        help="Output HTML file path")
    args = parser.parse_args()

    if not args.token:
        print("ERROR: provide --token or set SONAR_TOKEN", file=sys.stderr)
        sys.exit(1)

    print("Fetching quality gate...")
    qg = fetch_quality_gate(args.token)

    print("Fetching metrics...")
    metrics = fetch_metrics(args.token)

    print("Fetching vulnerabilities...")
    vulns = fetch_issues("VULNERABILITY", args.token)

    print("Fetching bugs...")
    bugs = fetch_issues("BUG", args.token)

    print("Fetching code smells...")
    smells = fetch_issues("CODE_SMELL", args.token)

    print("Fetching security hotspots...")
    hotspots = fetch_hotspots(args.token)

    print("Building report...")
    html = build_html(qg, metrics, vulns, bugs, hotspots, smells)

    with open(args.output, "w", encoding="utf-8") as f:
        f.write(html)

    print(f"Report saved to: {args.output}")


if __name__ == "__main__":
    main()
