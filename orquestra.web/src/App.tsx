import './App.css'

const recursos = [
  {
    numero: '01',
    titulo: 'Inventário organizado',
    descricao:
      'Cadastre equipamentos, categorias e departamentos e encontre cada ativo sem depender de planilhas espalhadas.',
  },
  {
    numero: '02',
    titulo: 'Custódias rastreáveis',
    descricao:
      'Registre entregas e devoluções, saiba quem está com cada equipamento e mantenha todo o histórico disponível.',
  },
  {
    numero: '03',
    titulo: 'Ciclo de vida completo',
    descricao:
      'Acompanhe manutenções, defeitos, descartes e desligamentos sem perder nenhuma etapa do processo.',
  },
]

function App() {
  return (
    <main>
      <header className="cabecalho">
        <a className="marca" href="#inicio" aria-label="Ir para o início">
          <span className="marca-simbolo" aria-hidden="true">
            O
          </span>
          <span>Orquestra</span>
        </a>

        <nav aria-label="Navegação principal">
          <a href="#como-funciona">Como funciona</a>
          <a href="#recursos">Recursos</a>
        </nav>
      </header>

      <section className="apresentacao" id="inicio">
        <div className="apresentacao-conteudo">
          <p className="chamada">Gestão de ativos de TI</p>
          <h1>Todos os equipamentos da empresa sob controle.</h1>
          <p className="resumo">
            O Orquestra reúne inventário, responsáveis e movimentações em um só
            lugar. Assim, a equipe sabe onde está cada ativo e o que aconteceu
            com ele ao longo do tempo.
          </p>
          <a className="botao-principal" href="#recursos">
            Conhecer o Orquestra
          </a>
        </div>

        <div className="painel-resumo" aria-label="Resumo do controle de ativos">
          <div className="painel-topo">
            <span>Exemplo de visão geral</span>
            <span className="situacao">Dados ilustrativos</span>
          </div>
          <div className="indicador-destaque">
            <span>Ativos acompanhados</span>
            <strong>248</strong>
          </div>
          <div className="indicadores">
            <div>
              <span className="ponto disponivel" />
              <p>Disponíveis</p>
              <strong>84</strong>
            </div>
            <div>
              <span className="ponto em-uso" />
              <p>Em uso</p>
              <strong>151</strong>
            </div>
            <div>
              <span className="ponto manutencao" />
              <p>Manutenção</p>
              <strong>13</strong>
            </div>
          </div>
        </div>
      </section>

      <section className="como-funciona" id="como-funciona">
        <p className="chamada">Do cadastro ao descarte</p>
        <h2>O histórico acompanha o ativo durante toda a sua vida útil.</h2>
        <p>
          Cada entrega, devolução ou mudança de situação fica registrada. Isso
          facilita conferências, evita dúvidas sobre responsabilidades e dá mais
          segurança aos processos internos.
        </p>
      </section>

      <section className="recursos" id="recursos" aria-labelledby="titulo-recursos">
        <div className="secao-titulo">
          <p className="chamada">O que o sistema resolve</p>
          <h2 id="titulo-recursos">Informação clara para quem cuida da operação.</h2>
        </div>
        <div className="grade-recursos">
          {recursos.map((recurso) => (
            <article key={recurso.numero}>
              <span>{recurso.numero}</span>
              <h3>{recurso.titulo}</h3>
              <p>{recurso.descricao}</p>
            </article>
          ))}
        </div>
      </section>

      <footer>
        <span>Orquestra</span>
        <p>Controle de ativos de TI com organização e rastreabilidade.</p>
      </footer>
    </main>
  )
}

export default App
