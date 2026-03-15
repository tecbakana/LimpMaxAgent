-- ============================================================
-- Script de criação do banco de dados LimpMax
-- Execute no SQL Server Management Studio
-- ============================================================

CREATE DATABASE LimpMaxDB;
GO

USE LimpMaxDB;
GO

-- Clientes
CREATE TABLE Clientes (
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    Nome     NVARCHAR(200) NOT NULL,
    CNPJ     NVARCHAR(18)  NOT NULL UNIQUE,
    Email    NVARCHAR(200) NOT NULL,
    Telefone NVARCHAR(20)  NOT NULL
);

-- Produtos
CREATE TABLE Produtos (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Nome          NVARCHAR(200)  NOT NULL,
    Descricao     NVARCHAR(500)  NOT NULL,
    Preco         DECIMAL(10, 2) NOT NULL,
    UnidadeMedida NVARCHAR(10)   NOT NULL DEFAULT 'un',  -- un, cx, fardo, L
    Ativo         BIT            NOT NULL DEFAULT 1
);

-- Estoque
CREATE TABLE Estoque (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    ProdutoId         INT NOT NULL REFERENCES Produtos(Id),
    Quantidade        INT NOT NULL DEFAULT 0,
    UltimaAtualizacao DATETIME NOT NULL DEFAULT GETDATE()
);

-- Pedidos
CREATE TABLE Pedidos (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId  INT            NOT NULL REFERENCES Clientes(Id),
    DataPedido DATETIME       NOT NULL DEFAULT GETDATE(),
    Status     NVARCHAR(20)   NOT NULL DEFAULT 'Pendente',  -- Pendente, Confirmado, Entregue, Cancelado
    ValorTotal DECIMAL(10, 2) NOT NULL
);

-- Itens do Pedido
CREATE TABLE ItensPedido (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    PedidoId      INT            NOT NULL REFERENCES Pedidos(Id),
    ProdutoId     INT            NOT NULL REFERENCES Produtos(Id),
    Quantidade    INT            NOT NULL,
    PrecoUnitario DECIMAL(10, 2) NOT NULL
);
GO

-- ============================================================
-- Dados de exemplo
-- ============================================================

INSERT INTO Clientes (Nome, CNPJ, Email, Telefone) VALUES
('Mercado Bom Preço',   '12.345.678/0001-90', 'compras@bompreco.com.br',  '(11) 91234-5678'),
('Supermercado Central','98.765.432/0001-10', 'pedidos@central.com.br',   '(11) 99876-5432'),
('Padaria do Zé',       '11.222.333/0001-44', 'zedapadaria@gmail.com',    '(11) 98765-4321');

INSERT INTO Produtos (Nome, Descricao, Preco, UnidadeMedida) VALUES
('Detergente Neutro 500ml',    'Detergente neutro concentrado para louças',       2.50,  'un'),
('Detergente Neutro 5L',       'Detergente neutro galão 5 litros',                18.90, 'un'),
('Desinfetante Lavanda 1L',    'Desinfetante perfumado lavanda',                  4.80,  'un'),
('Desinfetante Pinho 5L',      'Desinfetante pinho galão 5 litros',               22.00, 'un'),
('Água Sanitária 2L',          'Água sanitária cloro ativo 2,0%',                 6.00,  'un'),
('Sabão em Pó 1kg',            'Sabão em pó para roupas brancas e coloridas',     12.50, 'un'),
('Amaciante Concentrado 2L',   'Amaciante concentrado 2 litros',                  15.00, 'un'),
('Multiuso Spray 500ml',       'Limpador multiuso spray 500ml',                   8.90,  'un'),
('Esponja Dupla Face Cx/10',   'Caixa com 10 esponjas dupla face',                18.00, 'cx'),
('Luva Látex Multiuso Par',    'Luva de látex tamanho M',                          4.50, 'par');

INSERT INTO Estoque (ProdutoId, Quantidade) VALUES
(1, 1200), (2, 350), (3, 800), (4, 280),
(5, 500),  (6, 420), (7, 310), (8, 680),
(9, 90),   (10, 200);
GO
