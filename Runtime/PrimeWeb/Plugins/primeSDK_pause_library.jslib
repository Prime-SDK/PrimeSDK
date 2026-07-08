const primeSDK_pause_library = {

    primeSDK_pause_isPaused: function () {
        return !!(Module.PrimeSDK && Module.PrimeSDK.pause && Module.PrimeSDK.pause.isPaused);
    },

    primeSDK_pause_onPauseChange: function (onPauseChange_ptr) {
        const onPauseChange = (isPaused) => {
            Module.invokeMonoPCallback(-1, onPauseChange_ptr, isPaused);
        };
        const addCallback = () => {
            if (Module.PrimeSDK && Module.PrimeSDK.pause) {
                Module.PrimeSDK.pause.onPauseChange.add(onPauseChange);
            }
        };

        if (Module.PrimeSDK && Module.PrimeSDK.pause) {
            addCallback();
            return;
        }

        if (Module.waitForPrimeSDK) {
            Module.waitForPrimeSDK().then(addCallback);
            return;
        }

        const intervalId = setInterval(() => {
            if (Module.PrimeSDK && Module.PrimeSDK.pause) {
                clearInterval(intervalId);
                addCallback();
            }
        }, 100);
    },

    primeSDK_pause_showPauseInitialize: function (enabled) {
        const state = Module.primeSDKShowPause = Module.primeSDKShowPause || {
            enabled: false,
            initialized: false,
            visible: false,
            pendingShow: false,
            focusLost: false,
            manualContinue: null,
            overlay: null,
            style: null,
            cursorStyles: [],
            intervalId: null,
            updateOverlayBounds: null,
            requestedEnabled: 0,
            waitingForPrimeSDK: false,
            handlers: {}
        };

        state.requestedEnabled = enabled;

        if (!Module.PrimeSDK || !Module.PrimeSDK.pause) {
            if (!state.waitingForPrimeSDK) {
                const initializeWhenReady = () => {
                    state.waitingForPrimeSDK = false;
                };

                state.waitingForPrimeSDK = true;

                if (Module.waitForPrimeSDK) {
                    Module.waitForPrimeSDK().then(initializeWhenReady);
                }
                else {
                    const intervalId = setInterval(() => {
                        if (Module.PrimeSDK && Module.PrimeSDK.pause) {
                            clearInterval(intervalId);
                            initializeWhenReady();
                        }
                    }, 100);
                }
            }

            return;
        }

        const getLanguage = () => {
            const language = ((navigator.languages && navigator.languages[0]) || navigator.language || 'en').toLowerCase();
            return language.split('-')[0];
        };

        const getMessage = () => {
            const messages = {
                ru: 'Чтобы продолжить, кликни по этой области.',
                en: 'Click this area to continue.'
            };
            return messages[getLanguage()] || messages.en;
        };

        const getOverlayTarget = () => {
            const isValidTarget = (element) => {
                if (!element) {
                    return false;
                }

                const rect = element.getBoundingClientRect();
                return rect.width > 32 && rect.height > 32;
            };

            const isViewportRect = (rect) => {
                return Math.abs(rect.left) < 1
                    && Math.abs(rect.top) < 1
                    && Math.abs(rect.width - window.innerWidth) < 2
                    && Math.abs(rect.height - window.innerHeight) < 2;
            };

            const canvas = document.querySelector('#unity-canvas, canvas');
            if (!canvas) {
                return null;
            }

            const selectors = [
                '#unity-container',
                '.unity-container',
                '#game-container',
                '#gameContainer',
                '.game-container',
                '#webgl-content',
                '.webgl-content',
                '#unity-wrapper',
                '.unity-wrapper',
                '#game-wrapper',
                '.game-wrapper'
            ];

            for (const selector of selectors) {
                const target = canvas.closest(selector);
                if (isValidTarget(target)) {
                    return target;
                }
            }

            const canvasRect = canvas.getBoundingClientRect();
            let parent = canvas.parentElement;
            while (parent && parent !== document.body && parent !== document.documentElement) {
                const rect = parent.getBoundingClientRect();
                if (!isViewportRect(rect) && rect.width >= canvasRect.width * 0.95 && rect.height >= canvasRect.height * 0.95 && isValidTarget(parent)) {
                    return parent;
                }

                parent = parent.parentElement;
            }

            return null;
        };

        const applyOverlayBounds = (overlay) => {
            const target = getOverlayTarget();
            if (!target) {
                overlay.style.setProperty('inset', '0');
                overlay.style.removeProperty('left');
                overlay.style.removeProperty('top');
                overlay.style.removeProperty('width');
                overlay.style.removeProperty('height');
                return;
            }

            const rect = target.getBoundingClientRect();
            overlay.style.setProperty('inset', 'auto');
            overlay.style.setProperty('left', `${rect.left}px`);
            overlay.style.setProperty('top', `${rect.top}px`);
            overlay.style.setProperty('width', `${rect.width}px`);
            overlay.style.setProperty('height', `${rect.height}px`);
        };

        const ensureStyle = () => {
            if (state.style) {
                return;
            }

            state.style = document.createElement('style');
            state.style.id = 'prime-sdk-show-pause-style';
            state.style.textContent = [
                '#prime-sdk-show-pause {',
                '    position: fixed;',
                '    inset: 0;',
                '    z-index: 2147483647;',
                '    display: flex;',
                '    align-items: center;',
                '    justify-content: center;',
                '    padding: 28px;',
                '    box-sizing: border-box;',
                '    background: linear-gradient(180deg, rgba(0, 0, 0, 0.96) 0%, rgba(0, 0, 0, 0) 100%);',
                '    color: #f4f4f4;',
                '    cursor: pointer !important;',
                '    pointer-events: auto;',
                '    font-family: Inter, "Segoe UI", Arial, sans-serif;',
                '    user-select: none;',
                '}',
                '#prime-sdk-show-pause .prime-sdk-show-pause__text {',
                '    max-width: min(920px, calc(100vw - 48px));',
                '    margin: 0;',
                '    color: #ff9400;',
                '    font-size: clamp(18px, 2.1vw, 30px);',
                '    font-weight: 700;',
                '    line-height: 1.24;',
                '    letter-spacing: 0;',
                '    text-align: center;',
                '    text-shadow: 0 2px 10px rgba(0, 0, 0, 0.56), 0 0 18px rgba(255, 105, 0, 0.24);',
                '}',
                '#prime-sdk-show-pause .prime-sdk-show-pause__content {',
                '    display: flex;',
                '    flex-direction: column;',
                '    align-items: center;',
                '    justify-content: center;',
                '    gap: 18px;',
                '    pointer-events: none;',
                '    transform-origin: center center;',
                '}',
                '#prime-sdk-show-pause .prime-sdk-show-pause__play-icon {',
                '    position: relative;',
                '    width: clamp(264px, 28vw, 416px);',
                '    height: clamp(264px, 28vw, 416px);',
                '    display: flex;',
                '    align-items: center;',
                '    justify-content: center;',
                '    filter: drop-shadow(0 18px 40px rgba(0, 0, 0, 0.36));',
                '}',
                '#prime-sdk-show-pause .prime-sdk-show-pause__play-svg {',
                '    display: block;',
                '    width: 76%;',
                '    height: 76%;',
                '    overflow: visible;',
                '    filter: drop-shadow(10px 10px 0 rgba(0, 0, 0, 0.3));',
                '}',
                '#prime-sdk-show-pause .prime-sdk-show-pause__watermark {',
                '    position: absolute;',
                '    left: 34px;',
                '    top: 28px;',
                '    width: min(240px, 30vw);',
                '    height: auto;',
                '    pointer-events: auto;',
                '    cursor: default !important;',
                '    user-select: none;',
                '    -webkit-user-select: none;',
                '}',
                '#prime-sdk-show-pause .prime-sdk-show-pause__watermark-image {',
                '    display: block;',
                '    width: 100%;',
                '    height: auto;',
                '    pointer-events: none;',
                '    user-select: none;',
                '    -webkit-user-drag: none;',
                '}'
            ].join('\n');
            document.head.appendChild(state.style);
        };

        const saveCursor = (element) => {
            if (!element || state.cursorStyles.some((entry) => entry.element === element)) {
                return;
            }

            state.cursorStyles.push({
                element,
                value: element.style.getPropertyValue('cursor'),
                priority: element.style.getPropertyPriority('cursor')
            });
        };

        const forceCursorVisible = () => {
            try {
                if (document.pointerLockElement && document.exitPointerLock) {
                    document.exitPointerLock();
                }
            }
            catch (exception) {
                console.warn('PrimeSDK ShowPause pointer lock release failed', exception);
            }

            const canvas = document.querySelector('#unity-canvas, canvas');
            [document.documentElement, document.body, canvas].forEach((element) => {
                saveCursor(element);
                if (element) {
                    element.style.setProperty('cursor', 'pointer', 'important');
                }
            });
        };

        const restoreCursor = () => {
            state.cursorStyles.forEach((entry) => {
                if (!entry.element) {
                    return;
                }

                if (entry.value) {
                    entry.element.style.setProperty('cursor', entry.value, entry.priority);
                }
                else {
                    entry.element.style.removeProperty('cursor');
                }
            });
            state.cursorStyles = [];
        };

        const hide = () => {
            if (!state.visible) {
                return;
            }

            state.visible = false;
            state.pendingShow = false;
            state.manualContinue = null;

            if (state.overlay && state.overlay.parentElement) {
                state.overlay.parentElement.removeChild(state.overlay);
            }

            if (state.updateOverlayBounds) {
                window.removeEventListener('resize', state.updateOverlayBounds);
                window.removeEventListener('scroll', state.updateOverlayBounds, true);
                state.updateOverlayBounds = null;
            }

            state.overlay = null;
            restoreCursor();
        };

        const isWatermarkEvent = (event) => {
            if (!state.overlay) {
                return false;
            }

            const watermark = state.overlay.querySelector('.prime-sdk-show-pause__watermark');
            if (!watermark) {
                return false;
            }

            const rect = watermark.getBoundingClientRect();
            return event.clientX >= rect.left
                && event.clientX <= rect.right
                && event.clientY >= rect.top
                && event.clientY <= rect.bottom;
        };

        const show = (manualContinue) => {
            const manualMode = typeof manualContinue === 'function';
            if (!manualMode && !state.enabled) {
                return;
            }

            if (state.visible) {
                if (manualMode) {
                    state.manualContinue = manualContinue;
                }
                return;
            }

            state.manualContinue = manualMode ? manualContinue : null;
            ensureStyle();
            forceCursorVisible();

            const overlay = document.createElement('div');
            overlay.id = 'prime-sdk-show-pause';
            overlay.setAttribute('role', 'button');
            overlay.setAttribute('aria-label', getMessage());
            overlay.tabIndex = 0;
            overlay.innerHTML = `
                <div class="prime-sdk-show-pause__content">
                    <div class="prime-sdk-show-pause__play-icon" aria-hidden="true">
                        <svg class="prime-sdk-show-pause__play-svg" viewBox="0 0 256 256" focusable="false" aria-hidden="true">
                            <defs>
                                <linearGradient id="prime-sdk-show-pause-play-gradient" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="0%" stop-color="#ffbd00"></stop>
                                    <stop offset="52%" stop-color="#ff6400"></stop>
                                    <stop offset="100%" stop-color="#ff2410"></stop>
                                </linearGradient>
                            </defs>
                            <path fill="url(#prime-sdk-show-pause-play-gradient)" d="M92 55 Q70 42 70 70 L70 186 Q70 214 94 199 L198 139 Q218 128 198 117 Z"></path>
                        </svg>
                    </div>
                    <p class="prime-sdk-show-pause__text"></p>
                </div>
                <div class="prime-sdk-show-pause__watermark" aria-hidden="true">
                    <img class="prime-sdk-show-pause__watermark-image" src="data:image/png;base64,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" alt="" draggable="false">
                </div>
            `;
            overlay.querySelector('.prime-sdk-show-pause__text').textContent = getMessage();
            overlay.querySelector('.prime-sdk-show-pause__watermark').addEventListener('click', (event) => {
                event.preventDefault();
                event.stopPropagation();
            });
            overlay.querySelector('.prime-sdk-show-pause__watermark').addEventListener('mousedown', (event) => {
                event.preventDefault();
                event.stopPropagation();
            });
            overlay.addEventListener('click', (event) => {
                if (isWatermarkEvent(event)) {
                    event.preventDefault();
                    return;
                }

                if (state.manualContinue) {
                    const onContinue = state.manualContinue;
                    hide();
                    onContinue();
                    return;
                }

                if (window.focus) {
                    window.focus();
                }
            });
            overlay.addEventListener('keydown', (event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    if (state.manualContinue) {
                        const onContinue = state.manualContinue;
                        hide();
                        onContinue();
                        return;
                    }

                    if (window.focus) {
                        window.focus();
                    }
                }
            });

            document.body.appendChild(overlay);
            state.overlay = overlay;
            state.updateOverlayBounds = () => applyOverlayBounds(overlay);
            state.updateOverlayBounds();
            window.addEventListener('resize', state.updateOverlayBounds);
            window.addEventListener('scroll', state.updateOverlayBounds, true);
            state.visible = true;
            state.pendingShow = false;
        };

        state.showContinuePrompt = (onContinue) => {
            show(onContinue);
        };

        const setVisibleFromPause = (isPaused) => {
            if (!state.enabled) {
                return;
            }

            if (isPaused) {
                show();
                return;
            }

            hide();
        };

        const disable = () => {
            state.enabled = false;
            state.focusLost = false;
            state.pendingShow = false;
            hide();

            if (state.initialized) {
                Module.PrimeSDK.pause.onPauseChange.remove(state.handlers.onPauseChange);
            }

            state.initialized = false;
            state.handlers = {};
            state.intervalId = null;
        };

        if (enabled !== 1) {
            disable();
            return;
        }

        state.enabled = true;

        if (state.initialized) {
            return;
        }

        state.handlers.onPauseChange = setVisibleFromPause;
        Module.PrimeSDK.pause.onPauseChange.add(state.handlers.onPauseChange);
        setVisibleFromPause(Module.PrimeSDK.pause.isPaused);
        state.initialized = true;
    },

    primeSDK_pause_showContinuePrompt: function (senderId, onContinue_ptr) {
        const onContinue = () => {
            Module.invokeMonoPCallback(senderId, onContinue_ptr);
        };

        const showStandalonePrompt = () => {
            const getLanguage = () => {
                const language = ((navigator.languages && navigator.languages[0]) || navigator.language || 'en').toLowerCase();
                return language.split('-')[0];
            };

            const getMessage = () => {
                const messages = {
                    ru: 'Чтобы продолжить, кликни по этой области.',
                    en: 'Click this area to continue.'
                };
                return messages[getLanguage()] || messages.en;
            };

            const getOverlayTarget = () => {
                const isValidTarget = (element) => {
                    if (!element) {
                        return false;
                    }

                    const rect = element.getBoundingClientRect();
                    return rect.width > 32 && rect.height > 32;
                };

                const isViewportRect = (rect) => {
                    return Math.abs(rect.left) < 1
                        && Math.abs(rect.top) < 1
                        && Math.abs(rect.width - window.innerWidth) < 2
                        && Math.abs(rect.height - window.innerHeight) < 2;
                };

                const canvas = document.querySelector('#unity-canvas, canvas');
                if (!canvas) {
                    return null;
                }

                const selectors = [
                    '#unity-container',
                    '.unity-container',
                    '#game-container',
                    '#gameContainer',
                    '.game-container',
                    '#webgl-content',
                    '.webgl-content',
                    '#unity-wrapper',
                    '.unity-wrapper',
                    '#game-wrapper',
                    '.game-wrapper'
                ];

                for (const selector of selectors) {
                    const target = canvas.closest(selector);
                    if (isValidTarget(target)) {
                        return target;
                    }
                }

                const canvasRect = canvas.getBoundingClientRect();
                let parent = canvas.parentElement;
                while (parent && parent !== document.body && parent !== document.documentElement) {
                    const rect = parent.getBoundingClientRect();
                    if (!isViewportRect(rect) && rect.width >= canvasRect.width * 0.95 && rect.height >= canvasRect.height * 0.95 && isValidTarget(parent)) {
                        return parent;
                    }

                    parent = parent.parentElement;
                }

                return null;
            };

            const applyOverlayBounds = (overlay) => {
                const target = getOverlayTarget();
                if (!target) {
                    overlay.style.setProperty('inset', '0');
                    overlay.style.removeProperty('left');
                    overlay.style.removeProperty('top');
                    overlay.style.removeProperty('width');
                    overlay.style.removeProperty('height');
                    return;
                }

                const rect = target.getBoundingClientRect();
                overlay.style.setProperty('inset', 'auto');
                overlay.style.setProperty('left', `${rect.left}px`);
                overlay.style.setProperty('top', `${rect.top}px`);
                overlay.style.setProperty('width', `${rect.width}px`);
                overlay.style.setProperty('height', `${rect.height}px`);
            };

            const ensureStyle = () => {
                if (document.getElementById('prime-sdk-show-pause-style')) {
                    return;
                }

                const style = document.createElement('style');
                style.id = 'prime-sdk-show-pause-style';
                style.textContent = [
                    '#prime-sdk-show-pause {',
                    '    position: fixed;',
                    '    inset: 0;',
                    '    z-index: 2147483000;',
                    '    display: flex;',
                    '    align-items: center;',
                    '    justify-content: center;',
                    '    padding: 28px;',
                    '    box-sizing: border-box;',
                    '    background: linear-gradient(180deg, rgba(0, 0, 0, 0.96) 0%, rgba(0, 0, 0, 0) 100%);',
                    '    color: #f4f4f4;',
                    '    cursor: pointer !important;',
                    '    pointer-events: auto;',
                    '    font-family: Inter, "Segoe UI", Arial, sans-serif;',
                    '    user-select: none;',
                    '}',
                    '#prime-sdk-show-pause .prime-sdk-show-pause__content {',
                    '    display: flex;',
                    '    flex-direction: column;',
                    '    align-items: center;',
                    '    justify-content: center;',
                    '    gap: 18px;',
                    '    pointer-events: none;',
                    '}',
                    '#prime-sdk-show-pause .prime-sdk-show-pause__text {',
                    '    max-width: min(920px, calc(100vw - 48px));',
                    '    margin: 0;',
                    '    color: #ff9400;',
                    '    font-size: clamp(18px, 2.1vw, 30px);',
                    '    font-weight: 700;',
                    '    line-height: 1.24;',
                    '    letter-spacing: 0;',
                    '    text-align: center;',
                    '    text-shadow: 0 2px 10px rgba(0, 0, 0, 0.56), 0 0 18px rgba(255, 105, 0, 0.24);',
                    '}',
                    '#prime-sdk-show-pause .prime-sdk-show-pause__play-svg {',
                    '    display: block;',
                    '    width: clamp(180px, 22vw, 320px);',
                    '    height: clamp(180px, 22vw, 320px);',
                    '    overflow: visible;',
                    '    filter: drop-shadow(10px 10px 0 rgba(0, 0, 0, 0.3));',
                    '}',
                    '#prime-sdk-show-pause .prime-sdk-show-pause__watermark {',
                    '    position: absolute;',
                    '    left: 34px;',
                    '    top: 28px;',
                    '    color: #ff9400;',
                    '    font-size: clamp(28px, 5vw, 72px);',
                    '    font-weight: 900;',
                    '    letter-spacing: 0.08em;',
                    '    opacity: 0.9;',
                    '    pointer-events: none;',
                    '    text-shadow: 0 0 18px rgba(255, 105, 0, 0.22);',
                    '}',
                    '#prime-sdk-show-pause .prime-sdk-show-pause__watermark span {',
                    '    display: block;',
                    '    font-size: 0.24em;',
                    '    letter-spacing: 0.5em;',
                    '    text-align: center;',
                    '}'
                ].join('\n');
                document.head.appendChild(style);
            };

            const removeExistingOverlay = () => {
                const existingOverlay = document.getElementById('prime-sdk-show-pause');
                if (existingOverlay && existingOverlay.parentElement) {
                    existingOverlay.parentElement.removeChild(existingOverlay);
                }
            };

            if (!document.body) {
                setTimeout(showStandalonePrompt, 100);
                return;
            }

            ensureStyle();
            removeExistingOverlay();

            const overlay = document.createElement('div');
            overlay.id = 'prime-sdk-show-pause';
            overlay.setAttribute('role', 'button');
            overlay.setAttribute('aria-label', getMessage());
            overlay.tabIndex = 0;
            overlay.innerHTML = [
                '<div class="prime-sdk-show-pause__watermark">PRIME<span>PUBLISHING</span></div>',
                '<div class="prime-sdk-show-pause__content">',
                '    <svg class="prime-sdk-show-pause__play-svg" viewBox="0 0 256 256" focusable="false" aria-hidden="true">',
                '        <defs>',
                '            <linearGradient id="prime-sdk-show-pause-play-gradient" x1="0" y1="0" x2="0" y2="1">',
                '                <stop offset="0%" stop-color="#ffbd00"></stop>',
                '                <stop offset="52%" stop-color="#ff6400"></stop>',
                '                <stop offset="100%" stop-color="#ff2410"></stop>',
                '            </linearGradient>',
                '        </defs>',
                '        <path fill="url(#prime-sdk-show-pause-play-gradient)" d="M92 55 Q70 42 70 70 L70 186 Q70 214 94 199 L198 139 Q218 128 198 117 Z"></path>',
                '    </svg>',
                '    <p class="prime-sdk-show-pause__text"></p>',
                '</div>'
            ].join('');

            overlay.querySelector('.prime-sdk-show-pause__text').textContent = getMessage();

            const updateOverlayBounds = () => applyOverlayBounds(overlay);
            updateOverlayBounds();
            window.addEventListener('resize', updateOverlayBounds);
            window.addEventListener('scroll', updateOverlayBounds, true);

            let isClosed = false;
            const close = () => {
                if (isClosed) {
                    return;
                }

                isClosed = true;

                if (overlay.parentElement) {
                    overlay.parentElement.removeChild(overlay);
                }

                window.removeEventListener('resize', updateOverlayBounds);
                window.removeEventListener('scroll', updateOverlayBounds, true);
                onContinue();
            };

            overlay.addEventListener('click', close);
            overlay.addEventListener('keydown', (event) => {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    close();
                }
            });
            document.body.appendChild(overlay);
            overlay.focus();
        };

        if (Module.primeSDKShowPause && Module.primeSDKShowPause.showContinuePrompt) {
            Module.primeSDKShowPause.showContinuePrompt(onContinue);
            return;
        }

        const ensureReady = () => {
            if (Module.primeSDKShowPause && Module.primeSDKShowPause.showContinuePrompt) {
                Module.primeSDKShowPause.showContinuePrompt(onContinue);
                return;
            }

            showStandalonePrompt();
        };

        if (Module.PrimeSDK && Module.PrimeSDK.pause) {
            ensureReady();
            return;
        }

        if (Module.waitForPrimeSDK) {
            Module.waitForPrimeSDK().then(ensureReady);
            return;
        }

        const intervalId = setInterval(() => {
            if (Module.PrimeSDK && Module.PrimeSDK.pause) {
                clearInterval(intervalId);
                ensureReady();
            }
        }, 100);
    }

};
mergeInto(LibraryManager.library, primeSDK_pause_library);
