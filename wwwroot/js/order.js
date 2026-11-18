// The buttons change the form quantity. Razor still calculates each updated receipt.
const form = document.querySelector("[data-quote-url]");
const receipt = document.querySelector("#live-receipt");
const connection = document.querySelector("#receipt-connection");
let activeRequest;

const shotCount = form.querySelector("[data-shot-count]");
const shotButtons = form.querySelectorAll("[data-shot-step]");

if (shotCount) {
    const min = Number(shotCount.min);
    const max = Number(shotCount.max);

    function updateShotButtons() {
        const count = shotCount.valueAsNumber;
        for (const button of shotButtons) {
            const step = Number(button.dataset.shotStep);
            button.disabled = step < 0 ? count <= min : count >= max;
        }
    }

    for (const button of shotButtons) {
        button.addEventListener("click", () => {
            const count = Number.isFinite(shotCount.valueAsNumber) ? shotCount.valueAsNumber : min;
            const next = count + Number(button.dataset.shotStep);
            shotCount.value = Math.min(max, Math.max(min, next));
            shotCount.dispatchEvent(new Event("change", { bubbles: true }));
        });
    }

    shotCount.addEventListener("change", updateShotButtons);
    shotCount.addEventListener("input", updateShotButtons);
    updateShotButtons();
}

form.addEventListener("change", async () => {
    // A slow response must never overwrite a newer selection.
    activeRequest?.abort();
    const request = new AbortController();
    activeRequest = request;
    connection.hidden = true;
    receipt.setAttribute("aria-busy", "true");

    try {
        const response = await fetch(form.dataset.quoteUrl, {
            method: "POST",
            body: new FormData(form),
            signal: request.signal
        });

        if (!response.ok) {
            throw new Error("Receipt request failed.");
        }

        const html = await response.text();
        if (activeRequest === request) {
            const traceWasOpen = receipt.querySelector("details")?.open;
            receipt.innerHTML = html;
            const trace = receipt.querySelector("details");
            if (trace) {
                trace.open = Boolean(traceWasOpen);
            }
        }
    } catch (error) {
        if (activeRequest === request && error.name !== "AbortError") {
            connection.textContent = "Receipt could not refresh. Change a selection to retry.";
            connection.hidden = false;
        }
    } finally {
        if (activeRequest === request) {
            receipt.removeAttribute("aria-busy");
        }
    }
});
