// Filter the menu already on the page, including its item descriptions.
const search = document.querySelector("#menu-search");
const groups = document.querySelectorAll("[data-menu-group]");
const feedback = document.querySelector("#menu-results");
const empty = document.querySelector("#menu-no-matches");

function normalize(value) {
    return value.toLowerCase().replace(/['’]/g, "");
}

function filterMenu() {
    const words = normalize(search.value.trim()).split(/\s+/);
    let total = 0;

    for (const group of groups) {
        let matches = 0;
        for (const item of group.querySelectorAll("[data-menu-item]")) {
            const description = normalize(item.dataset.search);
            item.hidden = !words.every(word => description.includes(word));
            if (!item.hidden) {
                matches++;
            }
        }

        group.hidden = matches === 0;
        total += matches;
    }

    empty.hidden = total !== 0;
    feedback.textContent = `${total} menu ${total === 1 ? "item" : "items"} found.`;
}

search.addEventListener("input", filterMenu);
window.addEventListener("pageshow", filterMenu);
