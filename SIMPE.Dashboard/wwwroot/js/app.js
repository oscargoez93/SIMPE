document.addEventListener('DOMContentLoaded', () => {
    const loginForm = document.getElementById('loginForm');
    const loginBtn = document.getElementById('loginBtn');
    const btnText = loginBtn.querySelector('.btn-text');
    const loader = loginBtn.querySelector('.loader');
    const errorMessage = document.getElementById('errorMessage');
    
    // Elementos de Registro
    const registerForm = document.getElementById('registerForm');
    const registerBtn = document.getElementById('registerBtn');
    const showRegister = document.getElementById('showRegister');
    const showLogin = document.getElementById('showLogin');

    // Cambiar a vista de Registro
    showRegister.addEventListener('click', (e) => {
        e.preventDefault();
        loginForm.style.display = 'none';
        registerForm.style.display = 'flex';
        errorMessage.style.display = 'none';
    });

    // Cambiar a vista de Login
    showLogin.addEventListener('click', (e) => {
        e.preventDefault();
        registerForm.style.display = 'none';
        loginForm.style.display = 'flex';
        errorMessage.style.display = 'none';
    });

    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        // Esconder el error si estaba visible
        errorMessage.style.display = 'none';

        // Mostrar el estado de carga
        btnText.style.opacity = '0';
        loader.style.display = 'block';
        loginBtn.disabled = true;

        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;

        // Llamada a la API real
        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });
            
            if (response.ok) {
                const data = await response.json();
                // Éxito, redirigir al panel
                window.location.href = data.redirect || 'dashboard.html';
            } else {
                const errorText = await response.text();
                throw new Error(errorText || 'Credenciales incorrectas');
            }
            
        } catch (error) {
            // Mostrar error
            errorMessage.style.display = 'block';
            errorMessage.textContent = error.message;
            
            // Restablecer botón
            btnText.style.opacity = '1';
            loader.style.display = 'none';
            loginBtn.disabled = false;

            // Animar el error (re-trigger animation)
            errorMessage.style.animation = 'none';
            errorMessage.offsetHeight; /* trigger reflow */
            errorMessage.style.animation = null; 
        }
    });

    // Lógica de Registro (Simulada)
    registerForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        errorMessage.style.display = 'none';

        const name = document.getElementById('regName').value;
        const email = document.getElementById('regEmail').value;
        const pass = document.getElementById('regPassword').value;
        const confirmPass = document.getElementById('regConfirmPassword').value;

        if (pass !== confirmPass) {
            errorMessage.style.display = 'block';
            errorMessage.textContent = 'Las contraseñas no coinciden.';
            // Animación error
            errorMessage.style.animation = 'none';
            errorMessage.offsetHeight;
            errorMessage.style.animation = null;
            return;
        }

        const btnTextReg = registerBtn.querySelector('.btn-text');
        const loaderReg = registerBtn.querySelector('.loader');

        // Mostrar estado de carga
        btnTextReg.style.opacity = '0';
        loaderReg.style.display = 'block';
        registerBtn.disabled = true;

        try {
            const response = await fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fullName: name, email: email, password: pass })
            });

            if (response.ok) {
                const data = await response.json();
                
                // Mostrar mensaje de éxito en verde
                errorMessage.style.display = 'block';
                errorMessage.style.background = 'rgba(74, 222, 128, 0.1)';
                errorMessage.style.borderColor = 'rgba(74, 222, 128, 0.3)';
                errorMessage.style.color = '#4ade80';
                errorMessage.textContent = data.message || 'Registro exitoso. Espera aprobación.';
                
                // Limpiar formulario y resetear botón
                registerForm.reset();
                btnTextReg.style.opacity = '1';
                loaderReg.style.display = 'none';
                registerBtn.disabled = false;
            } else {
                const errorText = await response.text();
                throw new Error(errorText || 'Ocurrió un error al registrar.');
            }
        } catch (error) {
            errorMessage.style.display = 'block';
            errorMessage.style.background = 'rgba(239, 68, 68, 0.1)';
            errorMessage.style.borderColor = 'rgba(239, 68, 68, 0.3)';
            errorMessage.style.color = '#ef4444';
            errorMessage.textContent = error.message || 'Ocurrió un error al registrar.';
            
            btnTextReg.style.opacity = '1';
            loaderReg.style.display = 'none';
            registerBtn.disabled = false;
        }
    });
});
