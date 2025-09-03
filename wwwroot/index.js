const regForm = document.getElementById('regForm');
const regMsg = document.getElementById('regMsg');
const loginForm = document.getElementById('loginForm');
const loginMsg = document.getElementById('loginMsg');
const meBox = document.getElementById('meBox');
const btnMe = document.getElementById('btnMe');
const btnLogout = document.getElementById('btnLogout');
const logoutMsg = document.getElementById('logoutMsg');

async function post(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include', // ВАЖНО: чтобы браузер посылал/получал куки
        body: JSON.stringify(body)
    });
    const data = await res.json().catch(() => ({}));
    return { ok: res.ok, data };
}

regForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    regMsg.textContent = '';
    const username = document.getElementById('regUsername').value.trim();
    const email = document.getElementById('regEmail').value.trim();
    const password = document.getElementById('regPassword').value;
    const { ok, data } = await post('/register', {username, email, password });
    if (ok) { regMsg.textContent = data.message || 'OK'; regMsg.className = 'ok'; regForm.reset(); }
    else { regMsg.textContent = data.error || (data.errors || []).join(', ') || 'Ошибка'; regMsg.className = 'err'; }
});

loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    loginMsg.textContent = '';
    const username = document.getElementById('loginUsername').value.trim();
    const email = document.getElementById('loginEmail').value.trim();
    const password = document.getElementById('loginPassword').value;
    const rememberMe = document.getElementById('rememberMe').checked;
    const { ok, data } = await post('/login', { username, email, password, rememberMe });
    if (ok) { loginMsg.textContent = data.message || 'Ok'; loginMsg.className = 'ok'; }
    else { loginMsg.textContent = data.error || 'Error'; loginMsg.className = 'err'; }
});

btnMe.addEventListener('click', async () => {
    meBox.textContent = '';
    try {
        const res = await fetch('/me', { credentials: 'include' });
        const data = await res.json();
        if (res.ok) meBox.textContent = JSON.stringify(data, null, 2);
        else meBox.textContent = JSON.stringify(data, null, 2);
    } catch {
        meBox.textContent = 'Network error';
    }
});

btnLogout.addEventListener('click', async () => {
    const { ok, data } = await post('/logout', {});
    logoutMsg.textContent = ok ? (data.message || 'Ok') : 'Error';
    logoutMsg.className = ok ? 'ok' : 'err';
});