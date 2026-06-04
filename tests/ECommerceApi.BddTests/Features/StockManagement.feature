Feature: Reposicao de estoque do Emporio
  Para manter o catalogo renovado
  Como administrador
  Quero poder renovar o estoque manualmente, e impedir que viajantes comuns o facam

  Scenario: Administrador renova o estoque manualmente
    Given que estou autenticado como administrador
    When eu solicito a renovacao do estoque
    Then a renovacao e autorizada

  Scenario: Viajante comum nao pode renovar o estoque
    Given que estou autenticado como viajante
    When eu solicito a renovacao do estoque
    Then a renovacao e negada
