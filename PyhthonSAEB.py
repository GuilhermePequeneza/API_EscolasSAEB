# Código COMPLETO adaptado para SQL Server
import requests
import zipfile
import pandas as pd
import pyodbc  # ← Para SQL Server (não mysql.connector)
import os
import time

# ========================================
# PARTE 1: BAIXAR E DESCOMPACTAR
# ========================================

print("Iniciando downloads...")

arquivos = {
    'brasil': 'https://download.inep.gov.br/ideb/resultados/divulgacao_brasil_ideb_2023.zip',
    'anos_iniciais': 'https://download.inep.gov.br/ideb/resultados/divulgacao_anos_iniciais_escolas_2023.zip',
    'anos_finais': 'https://download.inep.gov.br/ideb/resultados/divulgacao_anos_finais_escolas_2023.zip',
    'ensino_medio': 'https://download.inep.gov.br/ideb/resultados/divulgacao_ensino_medio_escolas_2023.zip'
}

os.makedirs('dados_ideb', exist_ok=True)

for nome, url in arquivos.items():
    print(f"Baixando {nome}...")
    response = requests.get(url)
    zip_path = f'dados_ideb/{nome}.zip'
    
    with open(zip_path, 'wb') as file:
        file.write(response.content)
    
    print(f"Descompactando {nome}...")
    with zipfile.ZipFile(zip_path, 'r') as zip_ref:
        zip_ref.extractall(f'dados_ideb/{nome}/')
    
    os.remove(zip_path)
    print(f"✓ {nome} processado!")

print("\n✅ Todos os arquivos baixados!\n")

# ========================================
# PARTE 2: LER OS DADOS
# ========================================

print("Lendo dados...")

df_brasil = pd.read_csv('dados_ideb/brasil/divulgacao_brasil_ideb_2023.csv', 
                        sep=';', 
                        encoding='latin1')

print(f"✓ Brasil: {len(df_brasil)} linhas")
print("\nColunas disponíveis:")
print(df_brasil.columns.tolist())
print("\nPrimeiras linhas:")
print(df_brasil.head())

# ========================================
# PARTE 3: SALVAR NO SQL SERVER
# ========================================

print("\n\nConectando ao SQL Server...")

# Conectar ao SQL Server
conn = pyodbc.connect(
    'DRIVER={ODBC Driver 17 for SQL Server};'
    'SERVER=MSI;'
    'DATABASE=master;'  # Mude para seu banco depois
    'Trusted_Connection=yes;'
)

cursor = conn.cursor()
print("✓ Conectado!\n")

# Criar tabela (se não existir)
print("Criando tabela...")
cursor.execute("""
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ideb_brasil' AND xtype='U')
    CREATE TABLE ideb_brasil (
        id INT IDENTITY(1,1) PRIMARY KEY,
        ano INT,
        rede VARCHAR(50),
        uf VARCHAR(2),
        sigla_uf VARCHAR(2),
        regiao VARCHAR(50),
        ideb DECIMAL(3,1),
        meta DECIMAL(3,1)
    )
""")
conn.commit()
print("✓ Tabela criada!\n")

# Limpar dados antigos
print("Limpando dados antigos...")
cursor.execute("DELETE FROM ideb_brasil")
conn.commit()
print("✓ Limpeza concluída!\n")

# Inserir dados
print("Inserindo dados...")
contador = 0
for index, row in df_brasil.iterrows():
    cursor.execute("""
        INSERT INTO ideb_brasil (ano, rede, uf, sigla_uf, regiao, ideb, meta) 
        VALUES (?, ?, ?, ?, ?, ?, ?)
    """, (
        row.get('NU_ANO', 2023),
        row.get('REDE', ''),
        row.get('UF', ''),
        row.get('SIGLA_UF', ''),
        row.get('REGIAO', ''),
        row.get('VL_OBSERVADO_2023', None),
        row.get('VL_META_2023', None)
    ))
    contador += 1
    if contador % 100 == 0:
        print(f"   {contador} registros inseridos...")

conn.commit()
print(f"\n✓ {contador} registros inseridos!\n")

# Fechar conexão
cursor.close()
conn.close()

print("🎉 PROCESSO COMPLETO!")