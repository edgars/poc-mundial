-- Schema fiel à fonte legada (AD-3).
-- Ordem de autoridade: DDL SQL Server retido > estoq_structure.TXT > reg_log no PRG > MCP.
-- Larguras são contrato: um caractere a mais num código de barras deixa de casar.

CREATE TABLE dbo.usuario (
    id          INT IDENTITY(1,1) NOT NULL,            -- AD-2: surrogate para a API
    matric      CHAR(5)      NOT NULL,
    nome        CHAR(35)     NOT NULL,                 -- RK-d1a55f1103db
    senha_hash  VARCHAR(200) NULL,                     -- AD-7: era senha CHAR(6) em texto puro
    niv_usu     NCHAR(1)     NULL,
    loja        VARCHAR(5)   NULL,
    CONSTRAINT PK_usuario PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_usuario_matric UNIQUE (matric)       -- chave natural preservada
);

CREATE TABLE dbo.acesso (
    id         INT IDENTITY(1,1) NOT NULL,
    matric     CHAR(5)  NOT NULL,
    arquivo    CHAR(10) NOT NULL,                      -- F-4: nome da TABELA
    descri     CHAR(30) NOT NULL,                      -- RK-ea5a22eaf219
    alterar    BIT NOT NULL,                           -- RK-fa1ca141cf21
    incluir    BIT NOT NULL,                           -- RK-6022cae899fa
    excluir    BIT NOT NULL,                           -- RK-be780ff12c0e
    consultar  BIT NOT NULL,                           -- RK-04c918661d8d
    CONSTRAINT PK_acesso PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_acesso UNIQUE (matric, arquivo),
    CONSTRAINT FK_acesso_usuario FOREIGN KEY (matric) REFERENCES dbo.usuario (matric)
);

CREATE TABLE dbo.forne (
    id        INT IDENTITY(1,1) NOT NULL,
    codfor    CHAR(5)  NOT NULL,
    descri    CHAR(40) NOT NULL,
    cgc       CHAR(18) NOT NULL,                       -- RK-b3e7fcc26f3e
    cod_com   CHAR(5)  NOT NULL,                       -- RK-ef82abb7456c
    categ     CHAR(2)  NOT NULL,                       -- RK-b5da8c743238
    tiplog    CHAR(10) NOT NULL,                       -- RK-e74f29d4f922
    lograd    CHAR(40) NOT NULL,                       -- RK-2ce1876d83ad
    bairro    CHAR(25) NOT NULL,                       -- RK-1d4194439839
    cep       CHAR(9)  NOT NULL,                       -- RK-4697ebd74678
    cidade    CHAR(25) NOT NULL,                       -- RK-854f2452216e
    uf        CHAR(2)  NOT NULL,                       -- RK-98835efbf746
    inscr     CHAR(18) NOT NULL,                       -- RK-6aff3b12acb2
    situacao  CHAR(1)  NOT NULL,                       -- RK-16bc1acd7b74 (ver Q-8)
    data_grav DATETIME2 NOT NULL,                      -- RK-353ee013c009
    sub_trib  BIT NOT NULL,                            -- RK-37afeda868c2
    Mov_Est   BIT NOT NULL,                            -- RK-f2ca891c315f
    CONSTRAINT PK_forne PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_forne_codfor UNIQUE (codfor)
);

-- estoq: o DBF real tem 116 colunas. Modelamos as que a tela do legado toca (AD-3).
CREATE TABLE dbo.estoq (
    id         INT IDENTITY(1,1) NOT NULL,
    codigo     CHAR(5)  NOT NULL,
    descri     CHAR(60) NOT NULL,
    embalag    CHAR(10) NULL,
    embalqt    NUMERIC(9,4) NULL,
    codbarr    CHAR(13) NULL,                          -- EAN-13, unidade de venda
    codbarr2   CHAR(13) NULL,
    codbarr3   CHAR(13) NULL,
    barr_emb   CHAR(14) NULL,                          -- DUN-14, embalagem
    barr_emb2  CHAR(14) NULL,
    barr_emb3  CHAR(14) NULL,                          -- confirmado Character(14) na fonte
    CONSTRAINT PK_estoq PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_estoq_codigo UNIQUE (codigo)
);
CREATE INDEX IX_estoq_codbarr  ON dbo.estoq (codbarr);
CREATE INDEX IX_estoq_barr_emb ON dbo.estoq (barr_emb);

-- conferencia: cada linha é um ITEM da nota. A PK composta inclui codigo (AD-10).
CREATE TABLE dbo.conferencia (
    id             INT IDENTITY(1,1) NOT NULL,
    filial         CHAR(5)  NOT NULL,
    orig_des       CHAR(5)  NOT NULL,
    tipo_doc       CHAR(3)  NOT NULL,
    SERIE          CHAR(3)  NOT NULL,
    numero         CHAR(9)  NOT NULL,
    codigo         CHAR(5)  NOT NULL,
    itnf           DECIMAL(4,0) NULL,
    dun14          CHAR(14) NULL CONSTRAINT DF_conferencia_dun14 DEFAULT (''),
    data_mov       DATETIME2 NULL,
    data_conf      DATETIME2 NULL,
    QTD_NF         DECIMAL(10,3) NULL CONSTRAINT DF_conferencia_qt_nf DEFAULT (0),
    QTD_REC        DECIMAL(10,3) NULL CONSTRAINT DF_conferencia_qtd_rec DEFAULT (0),
    QTD_UNID_NF    DECIMAL(10,3) NULL CONSTRAINT DF_conferencia_QTD_UNID_NF DEFAULT (0),
    QTD_UNID_REC   DECIMAL(10,3) NULL CONSTRAINT DF_conferencia_QTD_UNID_REC DEFAULT (0),
    matr_conf      CHAR(5)  NULL CONSTRAINT DF_conferencia_matr_conf DEFAULT (''),
    matr_fec       CHAR(5)  NULL CONSTRAINT DF_conferencia_matr_fec DEFAULT (''),
    matr_lib       CHAR(5)  NULL CONSTRAINT DF_conferencia_matr_lib DEFAULT (' '),
    situacao       CHAR(1)  NULL CONSTRAINT DF_conferencia_situacao DEFAULT ('A'),
    status         BIT      NULL CONSTRAINT DF_conferencia_status DEFAULT (0),
    acesso         CHAR(25) NULL,                      -- o documento que o operador bipa
    fechado        BIT      NULL CONSTRAINT DF_conferencia_fechado DEFAULT (0),
    pendencia      BIT      NULL CONSTRAINT DF_conferencia_pendencia DEFAULT (0),
    doca           INT      NULL CONSTRAINT DF_conferencia_doca DEFAULT (0),
    finan          CHAR(1)  NULL CONSTRAINT DF_conferencia_finan DEFAULT ('N'),
    dt_hora        DATETIME2 NULL,
    codfor         CHAR(5)  NULL,
    peso_bruto_col DECIMAL(11,3) NOT NULL CONSTRAINT DF_conferencia_peso DEFAULT (0),
    balanca        BIT NOT NULL CONSTRAINT DF_conferencia_balanca DEFAULT (0),
    versao         ROWVERSION NOT NULL,                -- AD-17: concorrência otimista
    CONSTRAINT PK_conferencia PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UQ_conferencia UNIQUE (filial, orig_des, tipo_doc, SERIE, numero, codigo)
);
CREATE INDEX IX_conferencia_doc ON dbo.conferencia (filial, orig_des, tipo_doc, SERIE, numero);
CREATE INDEX IX_conferencia_acesso ON dbo.conferencia (acesso);

-- log_even: schema recuperado da função reg_log em conferencia.PRG (F-8).
CREATE TABLE dbo.log_even (
    id        INT IDENTITY(1,1) NOT NULL,
    data_eve  DATETIME2    NOT NULL,
    usuario   CHAR(5)      NULL,
    arquivo   VARCHAR(30)  NULL,
    chave     VARCHAR(100) NULL,
    val_ant   VARCHAR(MAX) NULL,
    val_atu   VARCHAR(MAX) NULL,
    CONSTRAINT PK_log_even PRIMARY KEY CLUSTERED (id)
);
CREATE INDEX IX_log_even_data ON dbo.log_even (data_eve DESC);
