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

// ============================================================================
// Virtualization Support (Experimental)
// ============================================================================

// Track active scroll subscriptions
const scrollSubscriptions = new Map();
let scrollSubscriptionId = 0;

/**
 * Gets the current scroll position of an element.
 *
 * @param {HTMLElement} element - The scrollable element
 * @param {boolean} isHorizontal - Whether to get horizontal (scrollLeft) or vertical (scrollTop) position
 * @returns {number} The current scroll position in pixels
 */
export function getScrollPosition(element, isHorizontal) {
    if (!element) return 0;
    return isHorizontal ? element.scrollLeft : element.scrollTop;
}

/**
 * Subscribes to scroll events on an element with requestAnimationFrame throttling.
 * Returns a subscription ID that can be used to unsubscribe.
 *
 * @param {HTMLElement} element - The scrollable element to observe
 * @param {DotNetObjectReference} dotNetRef - Reference to the Blazor component
 * @param {boolean} isHorizontal - Whether to track horizontal or vertical scrolling
 * @returns {number} Subscription ID for cleanup
 */
export function subscribeToScroll(element, dotNetRef, isHorizontal) {
    if (!element || !dotNetRef) {
        console.warn('subscribeToScroll: Missing required parameters');
        return -1;
    }

    const id = ++scrollSubscriptionId;
    let ticking = false;

    const handler = () => {
        if (!ticking) {
            requestAnimationFrame(() => {
                const pos = isHorizontal ? element.scrollLeft : element.scrollTop;
                try {
                    dotNetRef.invokeMethodAsync('OnScrollPositionChanged', pos);
                } catch (error) {
                    console.debug('Failed to invoke scroll callback:', error);
                }
                ticking = false;
            });
            ticking = true;
        }
    };

    element.addEventListener('scroll', handler, { passive: true });

    scrollSubscriptions.set(id, {
        element,
        handler,
        dotNetRef
    });

    return id;
}

/**
 * Unsubscribes from scroll events using the subscription ID.
 *
 * @param {number} subscriptionId - The ID returned from subscribeToScroll
 */
export function unsubscribeFromScroll(subscriptionId) {
    const subscription = scrollSubscriptions.get(subscriptionId);
    if (subscription) {
        subscription.element.removeEventListener('scroll', subscription.handler);
        scrollSubscriptions.delete(subscriptionId);
    }
}

/**
 * Gets the current scroll position for both axes (for 2D virtualization).
 *
 * @param {HTMLElement} element - The scrollable element
 * @returns {{ scrollLeft: number, scrollTop: number }} Current scroll positions
 */
export function getScrollPosition2D(element) {
    if (!element) return { scrollLeft: 0, scrollTop: 0 };
    return {
        scrollLeft: element.scrollLeft,
        scrollTop: element.scrollTop
    };
}

/**
 * Subscribes to scroll events for 2D virtualization (both axes).
 *
 * @param {HTMLElement} element - The scrollable element to observe
 * @param {DotNetObjectReference} dotNetRef - Reference to the Blazor component
 * @returns {number} Subscription ID for cleanup
 */
export function subscribeToScroll2D(element, dotNetRef) {
    if (!element || !dotNetRef) {
        console.warn('subscribeToScroll2D: Missing required parameters');
        return -1;
    }

    const id = ++scrollSubscriptionId;
    let ticking = false;

    const handler = () => {
        if (!ticking) {
            requestAnimationFrame(() => {
                try {
                    dotNetRef.invokeMethodAsync('OnScrollPositionChanged2D', element.scrollLeft, element.scrollTop);
                } catch (error) {
                    console.debug('Failed to invoke 2D scroll callback:', error);
                }
                ticking = false;
            });
            ticking = true;
        }
    };

    element.addEventListener('scroll', handler, { passive: true });

    scrollSubscriptions.set(id, {
        element,
        handler,
        dotNetRef
    });

    return id;
}
