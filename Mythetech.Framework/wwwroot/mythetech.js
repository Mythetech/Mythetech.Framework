/**
 * Mythetech Framework JavaScript Module
 *
 * This module provides JavaScript interop functionality for the Mythetech Framework,
 * including settings panel scroll-spy behavior.
 */

// Track the current scroll-spy state
let scrollSpyState = null;

/**
 * Sets up scroll-spy functionality for the settings panel.
 * Detects which section is currently visible and notifies the Blazor component.
 *
 * @param {HTMLElement} containerElement - The scrollable container element
 * @param {DotNetObjectReference} dotNetRef - Reference to the Blazor component
 * @param {string[]} sectionIds - Array of section IDs to observe
 */
export function setupScrollSpy(containerElement, dotNetRef, sectionIds) {
    // Clean up any existing scroll-spy
    teardownScrollSpy();

    if (!containerElement || !dotNetRef || !sectionIds?.length) {
        console.warn('setupScrollSpy: Missing required parameters');
        return;
    }

    // Create intersection observer options
    const options = {
        root: containerElement,
        rootMargin: '-100px 0px -80% 0px',
        threshold: 0
    };

    // Create observer that detects when sections enter the viewport
    const observer = new IntersectionObserver((entries) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                const sectionId = entry.target.id.replace('section-', '');
                try {
                    dotNetRef.invokeMethodAsync('UpdateActiveSection', sectionId);
                } catch (error) {
                    // Component may have been disposed
                    console.debug('Failed to update active section:', error);
                }
                break;
            }
        }
    }, options);

    // Observe all section elements
    const observedElements = [];
    for (const sectionId of sectionIds) {
        const element = document.getElementById(`section-${sectionId}`);
        if (element) {
            observer.observe(element);
            observedElements.push(element);
        }
    }

    // Also observe any custom sections in the container
    const allSections = containerElement.querySelectorAll('[id^="section-"]');
    for (const section of allSections) {
        if (!observedElements.includes(section)) {
            observer.observe(section);
            observedElements.push(section);
        }
    }

    // Store state for cleanup
    scrollSpyState = {
        observer,
        dotNetRef,
        observedElements
    };

    // Trigger initial check
    setTimeout(() => {
        const firstSection = observedElements[0];
        if (firstSection) {
            const sectionId = firstSection.id.replace('section-', '');
            try {
                dotNetRef.invokeMethodAsync('UpdateActiveSection', sectionId);
            } catch (error) {
                console.debug('Failed to set initial section:', error);
            }
        }
    }, 100);
}

/**
 * Smoothly scrolls to a specific section within the settings panel.
 *
 * @param {string} sectionId - The ID of the section to scroll to (without 'section-' prefix)
 */
export function scrollToSection(sectionId) {
    const element = document.getElementById(`section-${sectionId}`);
    if (element) {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
}

/**
 * Cleans up the scroll-spy observer and releases resources.
 */
export function teardownScrollSpy() {
    if (scrollSpyState) {
        if (scrollSpyState.observer) {
            scrollSpyState.observer.disconnect();
        }
        scrollSpyState = null;
    }
}
