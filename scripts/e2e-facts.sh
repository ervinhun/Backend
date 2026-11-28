#!/usr/bin/env bash
# Checks FACTS about a running API, not just "did it answer". Used by the release gates and after every deploy.
#
#   bash scripts/e2e-facts.sh <base-url>
#
#   WRITE=1   also register a throw-away user and create data (only against a throw-away database)
#   WAIT=<s>  wait up to this many seconds for the API to answer first (a stopped Fly machine needs time to start)
set -uo pipefail

BASE="${1:?usage: e2e-facts.sh <base-url>}"
BASE="${BASE%/}"
WAIT="${WAIT:-0}"
failed=0

pass() { echo "  ok   $1"; }
fail() { echo "  FAIL $1"; failed=1; }
# status <method> <path> [curl args...]: prints the HTTP status code
status() { local m=$1 p=$2; shift 2; curl -s -o /dev/null -w '%{http_code}' -X "$m" "$@" "$BASE$p"; }
expect() { # expect <description> <wanted status> <method> <path> [curl args...]
  local d=$1 want=$2; shift 2
  local got; got=$(status "$@")
  [ "$got" = "$want" ] && pass "$d ($got)" || fail "$d: wanted $want, got $got"
}

if [ "$WAIT" -gt 0 ]; then
  for _ in $(seq 1 $((WAIT / 3))); do
    [ "$(status GET /)" = 200 ] && break
    sleep 3
  done
fi

echo "Facts about $BASE"

echo "- it is up"
[ "$(curl -s "$BASE/")" = "Hello World!" ] && pass "GET / answers" || fail "GET / does not answer 'Hello World!'"

echo "- data is locked without a token"
expect "fridges need a token" 401 GET /api/fridges
expect "product categories need a token" 401 GET /api/product-categories
expect "a made-up token is refused" 401 GET /api/auth/Me -H "Authorization: Bearer not-a-real-token"

echo "- nothing for developers is exposed"
expect "no Swagger UI" 404 GET /swagger/index.html
expect "no OpenAPI document" 404 GET /openapi/v1.json

echo "- the database answers (read-only)"
expect "an unknown user cannot log in" 401 POST /api/auth/Login \
  -H 'Content-Type: application/json' \
  -d '{"email":"nobody-'"$RANDOM"'@example.invalid","password":"wrong-password-123"}'

if [ "${WRITE:-0}" = 1 ]; then
  echo "- a user can sign up and use it (writes data)"
  email="gate-$(date +%s)-$RANDOM@example.com"
  auth=$(curl -s -X POST "$BASE/api/auth/Register" -H 'Content-Type: application/json' \
    -d '{"email":"'"$email"'","password":"Gate-password-123","displayName":"Release gate"}')
  token=$(jq -r '.accessToken // empty' <<<"$auth")
  [ -n "$token" ] && pass "register returns a token" || fail "register: $auth"
  H=(-H "Authorization: Bearer $token")

  [ "$(curl -s "${H[@]}" "$BASE/api/auth/Me" | jq -r .email)" = "$email" ] \
    && pass "the token belongs to the new user" || fail "/api/auth/Me does not return the new user"

  categories=$(curl -s "${H[@]}" "$BASE/api/product-categories" | jq 'length')
  [ "${categories:-0}" -gt 0 ] && pass "the food catalogue is seeded ($categories categories)" \
    || fail "no product categories: the seed data did not load"

  fridge=$(curl -s -X POST "${H[@]}" -H 'Content-Type: application/json' "$BASE/api/fridges" -d '{"name":"Gate fridge"}')
  fridge_id=$(jq -r '.id // empty' <<<"$fridge")
  [ -n "$fridge_id" ] && pass "a fridge can be created" || fail "create fridge: $fridge"
  curl -s "${H[@]}" "$BASE/api/fridges" | jq -e --arg id "$fridge_id" 'any(.[]; .id == $id)' >/dev/null \
    && pass "the new fridge is listed" || fail "the new fridge is not in GET /api/fridges"

  expect "another user cannot read it" 404 GET "/api/fridges/$fridge_id" \
    -H "Authorization: Bearer $(curl -s -X POST "$BASE/api/auth/Register" -H 'Content-Type: application/json' \
      -d '{"email":"other-'"$email"'","password":"Gate-password-123","displayName":"Other"}' | jq -r .accessToken)"
fi

[ "$failed" = 0 ] && echo "All facts hold." || { echo "Some facts do not hold."; exit 1; }
