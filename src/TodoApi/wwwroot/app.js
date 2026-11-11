const api = '/api/todos';
const $ = id => document.getElementById(id);

const nomesStatus = { Pendente: 'Pendente', EmAndamento: 'Em andamento', Concluido: 'Concluído' };

async function req(url, opts = {}) {
  const res = await fetch(url, {
    ...opts,
    headers: { 'Content-Type': 'application/json' }
  });

  if (!res.ok) {
    let msg = 'Erro ' + res.status;
    try {
      const p = await res.json();
      if (p.errors) msg = Object.values(p.errors).flat().join(' ');
      else msg = p.detail || p.title || msg;
    } catch { }
    throw new Error(msg);
  }

  return res.status === 204 ? null : res.json();
}

async function carregar() {
  const q = new URLSearchParams();
  if ($('fStatus').value) q.set('status', $('fStatus').value);
  if ($('fDe').value) q.set('dueDateFrom', $('fDe').value);
  if ($('fAte').value) q.set('dueDateTo', $('fAte').value);

  try {
    const tarefas = await req(api + '?' + q);
    render(tarefas);
    $('erro').textContent = '';
  } catch (e) {
    $('erro').textContent = e.message;
  }
}

function render(tarefas) {
  const ul = $('lista');
  ul.innerHTML = '';
  $('vazio').hidden = tarefas.length > 0;

  const agora = new Date();

  for (const t of tarefas) {
    const li = document.createElement('li');
    li.className = t.status;

    const venc = new Date(t.dueDate);
    const atrasada = t.status !== 'Concluido' && venc < agora;

    li.innerHTML = `
      <div class="topo">
        <h3></h3>
        <span class="info">${nomesStatus[t.status]}</span>
      </div>
      <p></p>
      <div class="info ${atrasada ? 'atrasada' : ''}">
        vence ${venc.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}${atrasada ? ' (atrasada)' : ''}
      </div>
      <div class="acoes">
        <select>
          ${Object.entries(nomesStatus).map(([v, n]) =>
            `<option value="${v}" ${v === t.status ? 'selected' : ''}>${n}</option>`).join('')}
        </select>
        <button class="sec" data-acao="editar">Editar</button>
        <button class="del" data-acao="excluir">Excluir</button>
      </div>`;

    // textContent pra nao injetar html vindo do usuario
    li.querySelector('h3').textContent = t.title;
    li.querySelector('p').textContent = t.description || '';
    if (!t.description) li.querySelector('p').remove();

    li.querySelector('select').onchange = e => mudarStatus(t.id, e.target.value);
    li.querySelector('[data-acao=editar]').onclick = () => editar(t);
    li.querySelector('[data-acao=excluir]').onclick = () => excluir(t.id);

    ul.appendChild(li);
  }
}

async function mudarStatus(id, status) {
  try {
    await req(`${api}/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) });
    carregar();
  } catch (e) {
    $('erro').textContent = e.message;
  }
}

async function excluir(id) {
  try {
    await req(`${api}/${id}`, { method: 'DELETE' });
    carregar();
  } catch (e) {
    $('erro').textContent = e.message;
  }
}

function editar(t) {
  $('id').value = t.id;
  $('title').value = t.title;
  $('description').value = t.description || '';
  $('status').value = t.status;
  $('dueDate').value = t.dueDate.slice(0, 16);
  $('salvar').textContent = 'Salvar';
  $('cancelar').hidden = false;
  $('title').focus();
}

function limparForm() {
  $('form').reset();
  $('id').value = '';
  $('salvar').textContent = 'Adicionar';
  $('cancelar').hidden = true;
}

$('form').onsubmit = async e => {
  e.preventDefault();

  const id = $('id').value;
  const body = JSON.stringify({
    title: $('title').value,
    description: $('description').value || null,
    status: $('status').value,
    dueDate: $('dueDate').value
  });

  try {
    if (id) await req(`${api}/${id}`, { method: 'PUT', body });
    else await req(api, { method: 'POST', body });
    limparForm();
    carregar();
  } catch (err) {
    $('erro').textContent = err.message;
  }
};

$('cancelar').onclick = limparForm;
$('fStatus').onchange = carregar;
$('fDe').onchange = carregar;
$('fAte').onchange = carregar;
$('limpar').onclick = () => {
  $('fStatus').value = '';
  $('fDe').value = '';
  $('fAte').value = '';
  carregar();
};

carregar();
