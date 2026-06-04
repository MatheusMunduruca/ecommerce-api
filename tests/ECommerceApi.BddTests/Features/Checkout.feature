Feature: Compra no Emporio do Rudolf
  Para adquirir itens de alquimia
  Como um viajante autenticado
  Quero adicionar itens a bolsa e selar o pedido

  Scenario: Selar um pedido debita o estoque
    Given que estou autenticado como viajante
    When eu adiciono 2 unidades de "Pocao de Cura Menor" a bolsa
    And eu selo o pedido
    Then o pedido e criado com sucesso
    And o estoque de "Pocao de Cura Menor" passa a ser 8

  Scenario: Nao posso comprar alem do estoque
    Given que estou autenticado como viajante
    When eu adiciono 5 unidades de "Erva da Lua" a bolsa
    Then a operacao e recusada
