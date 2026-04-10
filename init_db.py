import sqlite3
import os

def main():
    # Nos aseguramos de estar en el mismo directorio del script
    db_path = os.path.join(os.path.dirname(__file__), 'simpe.db')
    schema_path = os.path.join(os.path.dirname(__file__), 'schema.sql')

    with open(schema_path, 'r', encoding='utf-8') as f:
        schema = f.read()

    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.executescript(schema)
    conn.commit()
    conn.close()

    print(f"Base de datos creada exitosamente en {db_path} con las tablas solicitadas.")

if __name__ == "__main__":
    main()
